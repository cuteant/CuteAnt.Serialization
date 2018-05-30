﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CuteAnt;
using CuteAnt.Buffers;
using CuteAnt.Reflection;
using Hyperion;
using Hyperion.Extensions;

namespace MessagePack.Formatters
{
    public abstract class HyperionFormatterBase<T> : DynamicObjectTypeFormatterBase<T>
    {
        private const int c_initialBufferSize = 1024 * 64;
        private static readonly HashSet<Type> s_primitiveTypes = new HashSet<Type>(new[]
        {
            typeof(Int32), typeof(Int64), typeof(Int16), typeof(UInt32), typeof(UInt64), typeof(UInt16),
            typeof(Single), typeof(Double), typeof(Decimal), typeof(Byte), typeof(SByte), typeof(Char),
            typeof(Boolean), typeof(TimeSpan), typeof(DateTime), typeof(DateTimeOffset), typeof(String),
            typeof(Guid), typeof(CombGuid)
        });

        private readonly Serializer _serializer;
        //private readonly bool _preserveObjectReferences;

        protected HyperionFormatterBase(Func<FieldInfo, bool> fieldFilter = null,
            IComparer<FieldInfo> fieldInfoComparer = null, Func<Type, bool> isSupportedFieldType = null)
            : base(fieldFilter, fieldInfoComparer, isSupportedFieldType)
        {
            _serializer = new Serializer(new SerializerOptions(versionTolerance: false, preserveObjectReferences: true));
            //_preserveObjectReferences = true;
        }

        protected HyperionFormatterBase(SerializerOptions options, Func<FieldInfo, bool> fieldFilter = null,
            IComparer<FieldInfo> fieldInfoComparer = null, Func<Type, bool> isSupportedFieldType = null)
            : base(fieldFilter, fieldInfoComparer, isSupportedFieldType)
        {
            if (null == options) { throw new ArgumentNullException(nameof(options)); }

            _serializer = new Serializer(options.Clone(false, true));
            //_preserveObjectReferences = _serializer.Options.PreserveObjectReferences;
        }

        public override T Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return default;
            }

            var startOffset = offset;

            var actualType = MessagePackBinary.ReadNamedType(bytes, offset, out var typeSize, true);
            var serializedObject = MessagePackBinary.ReadBytesSegment(bytes, offset + typeSize, out readSize);
            readSize += typeSize;

            var obj = ActivatorUtils.FastCreateInstance(actualType);

            using (var pooledSession = DeserializerSessionManager.Create(_serializer))
            {
                var session = pooledSession.Object;
                session.TrackDeserializedObject(obj);
                session.TrackDeserializedType(actualType);

                using (var ms = new MemoryStream(serializedObject.Array, serializedObject.Offset, serializedObject.Count, false))
                {
                    var fields = GetFieldsFromCache(actualType);
                    foreach (var (field, getter, setter) in fields)
                    {
                        var fieldType = field.FieldType;
                        object fieldValue;
                        if (s_primitiveTypes.Contains(fieldType))
                        {
                            var valueSerializer = _serializer.GetSerializerByType(fieldType);
                            fieldValue = valueSerializer.ReadValue(ms, session);
                        }
                        else
                        {
                            var valueType = fieldType;
                            if (fieldType.IsNullableType())
                            {
                                valueType = Nullable.GetUnderlyingType(fieldType);
                            }
                            var valueSerializer = _serializer.GetSerializerByType(valueType);
                            fieldValue = ms.ReadObject(session);
                        }
                        setter(obj, fieldValue);
                    }
                }
            }

            return (T)obj;
        }

        public override int Serialize(ref byte[] bytes, int offset, T value, IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }

            var actualType = value.GetType();
            if (!IsSupportedType(actualType))
            {
                throw new InvalidOperationException($"Type '{actualType}' is an interface or abstract class and cannot be serialized.");
            }

            var typeSize = MessagePackBinary.WriteNamedType(ref bytes, offset, actualType);

            var bufferPool = BufferManager.Shared;
            byte[] buffer; int bufferSize;

            using (var pooledSession = SerializerSessionManager.Create(_serializer))
            {
                var session = pooledSession.Object;
                session.TrackSerializedType(actualType);
                session.TrackSerializedObject(value);

                using (var pooledStream = BufferManagerOutputStreamManager.Create())
                {
                    var outputStream = pooledStream.Object;
                    outputStream.Reinitialize(c_initialBufferSize, bufferPool);

                    var fields = GetFieldsFromCache(actualType);
                    foreach (var (field, getter, setter) in fields)
                    {
                        var fieldType = field.FieldType;
                        var v = GetFieldValue(value, field, getter);
                        if (s_primitiveTypes.Contains(fieldType))
                        {
                            var valueSerializer = _serializer.GetSerializerByType(fieldType);
                            valueSerializer.WriteValue(outputStream, v, session);
                        }
                        else
                        {
                            var valueType = fieldType;
                            if (fieldType.IsNullableType())
                            {
                                valueType = Nullable.GetUnderlyingType(fieldType);
                            }
                            var valueSerializer = _serializer.GetSerializerByType(valueType);
                            outputStream.WriteObject(v, valueType, valueSerializer, true, session);
                        }
                    }

                    buffer = outputStream.ToArray(out bufferSize);
                }
            }

            try
            {
                return MessagePackBinary.WriteBytes(ref bytes, offset + typeSize, buffer, 0, bufferSize) + typeSize;
            }
            finally { bufferPool.Return(buffer); }
        }
    }
}

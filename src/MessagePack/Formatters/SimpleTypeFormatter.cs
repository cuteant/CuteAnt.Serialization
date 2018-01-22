﻿using System;
using System.Collections.Generic;
using System.Text;
using CuteAnt.Reflection;

namespace MessagePack.Formatters
{
    public sealed class SimpleTypeFormatter : SimpleTypeFormatter<Type>
    {
        public static readonly SimpleTypeFormatter Instance = new SimpleTypeFormatter();
        public SimpleTypeFormatter() : base() { }

        public SimpleTypeFormatter(bool throwOnError) : base(throwOnError) { }
    }

    public class SimpleTypeFormatter<TType> : IMessagePackFormatter<TType>
        where TType : Type
    {
        private readonly bool _throwOnError;

        public SimpleTypeFormatter() : this(true) { }

        public SimpleTypeFormatter(bool throwOnError)
        {
            _throwOnError = throwOnError;
        }

        public TType Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }

            var hashCode = MessagePackBinary.ReadInt32(bytes, offset, out var hashCodeSize);
            offset += hashCodeSize;
            var typeName = MessagePackBinary.ReadBytes(bytes, offset, out readSize);
            readSize += hashCodeSize;
            return (TType)TypeSerializer.GetTypeFromTypeKey(new TypeKey(hashCode, typeName), _throwOnError);
        }

        public int Serialize(ref byte[] bytes, int offset, TType value, IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }

            var typeKey = TypeSerializer.GetTypeKeyFromType(value);
            var startOffset = offset;
            offset += MessagePackBinary.WriteInt32(ref bytes, offset, typeKey.HashCode);
            var typeName = typeKey.TypeName;
            offset += MessagePackBinary.WriteBytes(ref bytes, offset, typeName, 0, typeName.Length);
            return offset - startOffset;
        }
    }
}

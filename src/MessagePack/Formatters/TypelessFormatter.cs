﻿#if NETSTANDARD || NETFRAMEWORK

using MessagePack.Internal;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using CuteAnt.Reflection;

namespace MessagePack.Formatters
{
    /// <summary>
    /// For `object` field that holds derived from `object` value, ex: var arr = new object[] { 1, "a", new Model() };
    /// </summary>
    public class TypelessFormatter : IMessagePackFormatter<object>
    {
        public const sbyte ExtensionTypeCode = 100;

        static readonly Regex SubtractFullNameRegex = new Regex(@", Version=\d+.\d+.\d+.\d+, Culture=\w+, PublicKeyToken=\w+", RegexOptions.Compiled);

        delegate int SerializeMethod(object dynamicContractlessFormatter, ref byte[] bytes, int offset, object value, IFormatterResolver formatterResolver);
        delegate object DeserializeMethod(object dynamicContractlessFormatter, byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize);

        public static readonly IMessagePackFormatter<object> Instance = new TypelessFormatter();

        static readonly ThreadsafeTypeKeyHashTable<KeyValuePair<object, SerializeMethod>> serializers = new ThreadsafeTypeKeyHashTable<KeyValuePair<object, SerializeMethod>>();
        static readonly ThreadsafeTypeKeyHashTable<KeyValuePair<object, DeserializeMethod>> deserializers = new ThreadsafeTypeKeyHashTable<KeyValuePair<object, DeserializeMethod>>();
        static readonly ThreadsafeTypeKeyHashTable<KeyValuePair<Type, byte[]>> typeNameCache = new ThreadsafeTypeKeyHashTable<KeyValuePair<Type, byte[]>>();
        static readonly AsymmetricKeyHashTable<byte[], ArraySegment<byte>, Type> typeCache = new AsymmetricKeyHashTable<byte[], ArraySegment<byte>, Type>(new StringArraySegmentByteAscymmetricEqualityComparer());

        static readonly HashSet<string> blacklistCheck;
        static readonly HashSet<Type> useBuiltinTypes = new HashSet<Type>()
        {
            typeof(Boolean),
            // typeof(Char),
            typeof(SByte),
            typeof(Byte),
            typeof(Int16),
            typeof(UInt16),
            typeof(Int32),
            typeof(UInt32),
            typeof(Int64),
            typeof(UInt64),
            typeof(Single),
            typeof(Double),
            typeof(string),
            typeof(byte[]),

            // array should save there types.
            //typeof(Boolean[]),
            //typeof(Char[]),
            //typeof(SByte[]),
            //typeof(Int16[]),
            //typeof(UInt16[]),
            //typeof(Int32[]),
            //typeof(UInt32[]),
            //typeof(Int64[]),
            //typeof(UInt64[]),
            //typeof(Single[]),
            //typeof(Double[]),
            //typeof(string[]),

            typeof(Boolean?),
            // typeof(Char?),
            typeof(SByte?),
            typeof(Byte?),
            typeof(Int16?),
            typeof(UInt16?),
            typeof(Int32?),
            typeof(UInt32?),
            typeof(Int64?),
            typeof(UInt64?),
            typeof(Single?),
            typeof(Double?),
        };

        public static Func<string, Type> BindToType { get; set; }

        //static Type DefaultBindToType(string typeName)
        //{
        //    return Type.GetType(typeName, false);
        //}

        // mscorlib or System.Private.CoreLib
        static readonly bool isMscorlib = typeof(int).AssemblyQualifiedName.Contains("mscorlib");

        ///// <summary>
        ///// When type name does not have Version, Culture, Public token - sometimes can not find type, example - ExpandoObject
        ///// In that can set to `false`
        ///// </summary>
        //public static volatile bool RemoveAssemblyVersion = true;

        static TypelessFormatter()
        {
            blacklistCheck = new HashSet<string>()
            {
                "System.CodeDom.Compiler.TempFileCollection",
                "System.IO.FileSystemInfo",
                "System.Management.IWbemClassObjectFreeThreaded"
            };

            serializers.TryAdd(typeof(object), _ => new KeyValuePair<object, SerializeMethod>(null, (object p1, ref byte[] p2, int p3, object p4, IFormatterResolver p5) => 0));
            deserializers.TryAdd(typeof(object), _ => new KeyValuePair<object, DeserializeMethod>(null, (object p1, byte[] p2, int p3, IFormatterResolver p4, out int p5) =>
            {
                p5 = 0;
                return new object();
            }));

            BindToType = TypeUtils.ResolveType; // DefaultBindToType;
        }

        //// see:http://msdn.microsoft.com/en-us/library/w3f99sx1.aspx
        //// subtract Version, Culture and PublicKeyToken from AssemblyQualifiedName 
        //static string BuildTypeName(Type type)
        //{
        //    if (RemoveAssemblyVersion)
        //    {
        //        string full = type.AssemblyQualifiedName;

        //        var shortened = SubtractFullNameRegex.Replace(full, "");
        //        if (Type.GetType(shortened, false) == null)
        //        {
        //            // if type cannot be found with shortened name - use full name
        //            shortened = full;
        //        }

        //        return shortened;
        //    }
        //    else
        //    {
        //        return type.AssemblyQualifiedName;
        //    }
        //}

        public int Serialize(ref byte[] bytes, int offset, object value, IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }

            var type = value.GetType();

            Type expectedType;
            byte[] typeName;
            if (typeNameCache.TryGetValue(type, out var typeAndName))
            {
                expectedType = typeAndName.Key;
                typeName = typeAndName.Value;
            }
            else
            {
                if (blacklistCheck.Contains(type.FullName))
                {
                    ThrowHelper.ThrowInvalidOperationException_Blacklist(type);
                }

                var ti = type.GetTypeInfo();
                if (ti.IsAnonymous() || useBuiltinTypes.Contains(type))
                {
                    typeName = null;
                    expectedType = null;
                }
                else
                {
                    typeName = TranslateTypeName(type, out expectedType);
                }
                typeNameCache.TryAdd(type, new KeyValuePair<Type, byte[]>(expectedType, typeName));
            }

            if (typeName == null)
            {
                return Resolvers.TypelessFormatterFallbackResolver.Instance.GetFormatter<object>().Serialize(ref bytes, offset, value, formatterResolver);
            }

            // don't use GetOrAdd for avoid closure capture.
            if (!serializers.TryGetValue(expectedType, out var formatterAndDelegate))
            {
                lock (serializers) // double check locking...
                {
                    if (!serializers.TryGetValue(expectedType, out formatterAndDelegate))
                    {
                        var ti = expectedType.GetTypeInfo();

                        var formatter = formatterResolver.GetFormatterDynamic(expectedType);
                        if (formatter == null)
                        {
                            ThrowHelper.ThrowFormatterNotRegisteredException(expectedType, formatterResolver);
                        }

                        var formatterType = typeof(IMessagePackFormatter<>).GetCachedGenericType(expectedType);
                        var param0 = Expression.Parameter(typeof(object), "formatter");
                        var param1 = Expression.Parameter(typeof(byte[]).MakeByRefType(), "bytes");
                        var param2 = Expression.Parameter(typeof(int), "offset");
                        var param3 = Expression.Parameter(typeof(object), "value");
                        var param4 = Expression.Parameter(typeof(IFormatterResolver), "formatterResolver");

                        var serializeMethodInfo = formatterType.GetRuntimeMethod("Serialize", new[] { typeof(byte[]).MakeByRefType(), typeof(int), expectedType, typeof(IFormatterResolver) });

                        var body = Expression.Call(
                            Expression.Convert(param0, formatterType),
                            serializeMethodInfo,
                            param1,
                            param2,
                            ti.IsValueType ? Expression.Unbox(param3, expectedType) : Expression.Convert(param3, expectedType),
                            param4);

                        var lambda = Expression.Lambda<SerializeMethod>(body, param0, param1, param2, param3, param4).Compile();

                        formatterAndDelegate = new KeyValuePair<object, SerializeMethod>(formatter, lambda);
                        serializers.TryAdd(expectedType, formatterAndDelegate);
                    }
                }
            }

            // mark as extension with code 100
            var startOffset = offset;
            offset += 6; // mark will be written at the end, when size is known
            offset += MessagePackBinary.WriteStringBytes(ref bytes, offset, typeName);
            offset += formatterAndDelegate.Value(formatterAndDelegate.Key, ref bytes, offset, value, formatterResolver);
            MessagePackBinary.WriteExtensionFormatHeaderForceExt32Block(ref bytes, startOffset, ExtensionTypeCode, offset - startOffset - 6);
            return offset - startOffset;
        }

        protected virtual byte[] TranslateTypeName(Type actualType, out Type expectedType)
        {
            expectedType = actualType;
            return TypeSerializer.GetTypeKeyFromType(actualType).TypeName; // StringEncoding.UTF8.GetBytes(BuildTypeName(type));
        }

        public object Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }

            int startOffset = offset;
            var packType = MessagePackBinary.GetMessagePackType(bytes, offset);
            if (packType == MessagePackType.Extension)
            {
                var ext = MessagePackBinary.ReadExtensionFormatHeader(bytes, offset, out readSize);
                if (ext.TypeCode == ExtensionTypeCode)
                {
                    // it has type name serialized
                    offset += readSize;
                    var typeName = MessagePackBinary.ReadStringSegment(bytes, offset, out readSize);
                    offset += readSize;
                    var result = DeserializeByTypeName(typeName, bytes, offset, formatterResolver, out readSize);
                    offset += readSize;
                    readSize = offset - startOffset;
                    return result;
                }
            }

            // fallback
            return Resolvers.TypelessFormatterFallbackResolver.Instance.GetFormatter<object>().Deserialize(bytes, startOffset, formatterResolver, out readSize);
        }

        /// <summary>
        /// Does not support deserializing of anonymous types
        /// Type should be covered by preceeding resolvers in complex/standard resolver
        /// </summary>
        private object DeserializeByTypeName(in ArraySegment<byte> typeName, byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            // try get type with assembly name, throw if not found
            if (!typeCache.TryGetValue(typeName, out var type))
            {
                var buffer = new byte[typeName.Count];
                Buffer.BlockCopy(typeName.Array, typeName.Offset, buffer, 0, buffer.Length);
                var str = StringEncoding.UTF8.GetString(buffer);
                type = BindToType(str);
                if (type == null)
                {
                    if (isMscorlib && str.Contains("System.Private.CoreLib"))
                    {
                        str = str.Replace("System.Private.CoreLib", "mscorlib");
                        type = Type.GetType(str, true); // throw
                    }
                    else if (!isMscorlib && str.Contains("mscorlib"))
                    {
                        str = str.Replace("mscorlib", "System.Private.CoreLib");
                        type = Type.GetType(str, true); // throw
                    }
                    else
                    {
                        type = Type.GetType(str, true); // re-throw
                    }
                }
                typeCache.TryAdd(buffer, type);
            }

            if (!deserializers.TryGetValue(type, out var formatterAndDelegate))
            {
                lock (deserializers)
                {
                    if (!deserializers.TryGetValue(type, out formatterAndDelegate))
                    {
                        var ti = type.GetTypeInfo();

                        var formatter = formatterResolver.GetFormatterDynamic(type);
                        if (formatter == null)
                        {
                            ThrowHelper.ThrowFormatterNotRegisteredException(type, formatterResolver);
                        }

                        var formatterType = typeof(IMessagePackFormatter<>).GetCachedGenericType(type);
                        var param0 = Expression.Parameter(typeof(object), "formatter");
                        var param1 = Expression.Parameter(typeof(byte[]), "bytes");
                        var param2 = Expression.Parameter(typeof(int), "offset");
                        var param3 = Expression.Parameter(typeof(IFormatterResolver), "formatterResolver");
                        var param4 = Expression.Parameter(typeof(int).MakeByRefType(), "readSize");

                        var deserializeMethodInfo = formatterType.GetRuntimeMethod("Deserialize", new[] { typeof(byte[]), typeof(int), typeof(IFormatterResolver), typeof(int).MakeByRefType() });

                        var deserialize = Expression.Call(
                            Expression.Convert(param0, formatterType),
                            deserializeMethodInfo,
                            param1,
                            param2,
                            param3,
                            param4);

                        Expression body = deserialize;
                        if (ti.IsValueType)
                            body = Expression.Convert(deserialize, typeof(object));
                        var lambda = Expression.Lambda<DeserializeMethod>(body, param0, param1, param2, param3, param4).Compile();

                        formatterAndDelegate = new KeyValuePair<object, DeserializeMethod>(formatter, lambda);
                        deserializers.TryAdd(type, formatterAndDelegate);
                    }
                }
            }

            return formatterAndDelegate.Value(formatterAndDelegate.Key, bytes, offset, formatterResolver, out readSize);
        }
    }

}

#endif
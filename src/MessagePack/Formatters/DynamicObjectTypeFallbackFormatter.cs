﻿#if NETSTANDARD || DESKTOPCLR

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using CuteAnt.Reflection;

namespace MessagePack.Formatters
{
    public sealed class DynamicObjectTypeFallbackFormatter : IMessagePackFormatter<object>
    {
        delegate int SerializeMethod(object dynamicFormatter, ref byte[] bytes, int offset, object value, IFormatterResolver formatterResolver);

        readonly MessagePack.Internal.ThreadsafeTypeKeyHashTable<KeyValuePair<object, SerializeMethod>> serializers = new MessagePack.Internal.ThreadsafeTypeKeyHashTable<KeyValuePair<object, SerializeMethod>>();

        readonly IFormatterResolver[] innerResolvers;

        public DynamicObjectTypeFallbackFormatter(params IFormatterResolver[] innerResolvers)
        {
            this.innerResolvers = innerResolvers;
        }

        public int Serialize(ref byte[] bytes, int offset, object value, IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }

            var type = value.GetType();
            var ti = type.GetTypeInfo();

            if (type == typeof(object))
            {
                // serialize to empty map
                return MessagePackBinary.WriteMapHeader(ref bytes, offset, 0);
            }

            KeyValuePair<object, SerializeMethod> formatterAndDelegate;
            if (!serializers.TryGetValue(type, out formatterAndDelegate))
            {
                lock (serializers)
                {
                    if (!serializers.TryGetValue(type, out formatterAndDelegate))
                    {
                        var aliasType = type;
                        var aliasTypeInfo = ti;
                        if (typeof(Type).IsAssignableFrom(type))
                        {
                            aliasType = typeof(Type);
                            aliasTypeInfo = typeof(Type).GetTypeInfo();
                        }
                        else if (typeof(ConstructorInfo).IsAssignableFrom(type))
                        {
                            aliasType = typeof(ConstructorInfo);
                            aliasTypeInfo = typeof(ConstructorInfo).GetTypeInfo();
                        }
                        else if (typeof(EventInfo).IsAssignableFrom(type))
                        {
                            aliasType = typeof(EventInfo);
                            aliasTypeInfo = typeof(EventInfo).GetTypeInfo();
                        }
                        else if (typeof(FieldInfo).IsAssignableFrom(type))
                        {
                            aliasType = typeof(FieldInfo);
                            aliasTypeInfo = typeof(FieldInfo).GetTypeInfo();
                        }
                        else if (typeof(PropertyInfo).IsAssignableFrom(type))
                        {
                            aliasType = typeof(PropertyInfo);
                            aliasTypeInfo = typeof(PropertyInfo).GetTypeInfo();
                        }
                        else if (typeof(MethodInfo).IsAssignableFrom(type))
                        {
                            aliasType = typeof(MethodInfo);
                            aliasTypeInfo = typeof(MethodInfo).GetTypeInfo();
                        }
                        else if (typeof(MemberInfo).IsAssignableFrom(type))
                        {
                            aliasType = typeof(MemberInfo);
                            aliasTypeInfo = typeof(MemberInfo).GetTypeInfo();
                        }
                        else if (typeof(Delegate).IsAssignableFrom(type))
                        {
                            aliasType = typeof(Delegate);
                            aliasTypeInfo = typeof(Delegate).GetTypeInfo();
                        }
                        else if (typeof(Exception).IsAssignableFrom(type))
                        {
                            aliasType = typeof(Exception);
                            aliasTypeInfo = typeof(Exception).GetTypeInfo();
                        }
                        else if (typeof(Expression).IsAssignableFrom(type))
                        {
                            aliasType = typeof(Expression);
                            aliasTypeInfo = typeof(Expression).GetTypeInfo();
                        }
                        else if (typeof(SymbolDocumentInfo).IsAssignableFrom(type))
                        {
                            aliasType = typeof(SymbolDocumentInfo);
                            aliasTypeInfo = typeof(SymbolDocumentInfo).GetTypeInfo();
                        }
                        else if (typeof(MemberBinding).IsAssignableFrom(type))
                        {
                            aliasType = typeof(MemberBinding);
                            aliasTypeInfo = typeof(MemberBinding).GetTypeInfo();
                        }
                        object formatter = null;
                        foreach (var innerResolver in innerResolvers)
                        {
                            formatter = innerResolver.GetFormatterDynamic(aliasType);
                            if (formatter != null) break;
                        }
                        if (formatter == null)
                        {
                            ThrowHelper.ThrowFormatterNotRegisteredException(type, innerResolvers);
                        }

                        var t = aliasType;
                        {
                            var formatterType = typeof(IMessagePackFormatter<>).GetCachedGenericType(t);
                            var param0 = Expression.Parameter(typeof(object), "formatter");
                            var param1 = Expression.Parameter(typeof(byte[]).MakeByRefType(), "bytes");
                            var param2 = Expression.Parameter(typeof(int), "offset");
                            var param3 = Expression.Parameter(typeof(object), "value");
                            var param4 = Expression.Parameter(typeof(IFormatterResolver), "formatterResolver");

                            var serializeMethodInfo = formatterType.GetRuntimeMethod("Serialize", new[] { typeof(byte[]).MakeByRefType(), typeof(int), t, typeof(IFormatterResolver) });

                            var body = Expression.Call(
                                Expression.Convert(param0, formatterType),
                                serializeMethodInfo,
                                param1,
                                param2,
                                aliasTypeInfo.IsValueType ? Expression.Unbox(param3, t) : Expression.Convert(param3, t),
                                param4);

                            var lambda = Expression.Lambda<SerializeMethod>(body, param0, param1, param2, param3, param4).Compile();

                            formatterAndDelegate = new KeyValuePair<object, SerializeMethod>(formatter, lambda);
                        }

                        serializers.TryAdd(type, formatterAndDelegate);
                    }
                }
            }

            return formatterAndDelegate.Value(formatterAndDelegate.Key, ref bytes, offset, value, formatterResolver);
        }

        public object Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            return PrimitiveObjectFormatter.Instance.Deserialize(bytes, offset, formatterResolver, out readSize);
        }
    }
}

#endif
﻿using System.Reflection;
using System.Collections;
using System.Linq;
using Utf8Json.Internal;
using System;
using CuteAnt.Reflection;
using Utf8Json.Formatters;
using System.Collections.Generic;
using Utf8Json.Resolvers.Internal;
using System.Collections.ObjectModel;

#if NETSTANDARD || DESKTOPCLR
using System.Threading.Tasks;
#endif

namespace Utf8Json.Resolvers
{
    public sealed class DynamicGenericResolver : IJsonFormatterResolver
    {
        public static readonly IJsonFormatterResolver Instance = new DynamicGenericResolver();

        DynamicGenericResolver()
        {

        }

        public IJsonFormatter<T> GetFormatter<T>()
        {
            return FormatterCache<T>.formatter;
        }

        static class FormatterCache<T>
        {
            public static readonly IJsonFormatter<T> formatter;

            static FormatterCache()
            {
                formatter = (IJsonFormatter<T>)DynamicGenericResolverGetFormatterHelper.GetFormatter(typeof(T));
            }
        }
    }
}

namespace Utf8Json.Resolvers.Internal
{
    internal static class DynamicGenericResolverGetFormatterHelper
    {
        static readonly Dictionary<Type, Type> formatterMap = new Dictionary<Type, Type>()
        {
            {typeof(List<>), typeof(ListFormatter<>)},
            {typeof(LinkedList<>), typeof(LinkedListFormatter<>)},
            {typeof(Queue<>), typeof(QeueueFormatter<>)},
            {typeof(Stack<>), typeof(StackFormatter<>)},
            {typeof(HashSet<>), typeof(HashSetFormatter<>)},
            {typeof(ReadOnlyCollection<>), typeof(ReadOnlyCollectionFormatter<>)},
            {typeof(IList<>), typeof(InterfaceListFormatter<>)},
            {typeof(ICollection<>), typeof(InterfaceCollectionFormatter<>)},
            {typeof(IEnumerable<>), typeof(InterfaceEnumerableFormatter<>)},
            {typeof(Dictionary<,>), typeof(DictionaryFormatter<,>)},
            {typeof(IDictionary<,>), typeof(InterfaceDictionaryFormatter<,>)},
            {typeof(SortedDictionary<,>), typeof(SortedDictionaryFormatter<,>)},
            {typeof(SortedList<,>), typeof(SortedListFormatter<,>)},
            {typeof(ILookup<,>), typeof(InterfaceLookupFormatter<,>)},
            {typeof(IGrouping<,>), typeof(InterfaceGroupingFormatter<,>)},
            #if NETSTANDARD || DESKTOPCLR
            {typeof(ObservableCollection<>), typeof(ObservableCollectionFormatter<>)},
            {typeof(ReadOnlyObservableCollection<>),(typeof(ReadOnlyObservableCollectionFormatter<>))},
#if !NET40
            {typeof(IReadOnlyList<>), typeof(InterfaceReadOnlyListFormatter<>)},
            {typeof(IReadOnlyCollection<>), typeof(InterfaceReadOnlyCollectionFormatter<>)},
#endif
            {typeof(ISet<>), typeof(InterfaceSetFormatter<>)},
            {typeof(System.Collections.Concurrent.ConcurrentBag<>), typeof(ConcurrentBagFormatter<>)},
            {typeof(System.Collections.Concurrent.ConcurrentQueue<>), typeof(ConcurrentQueueFormatter<>)},
            {typeof(System.Collections.Concurrent.ConcurrentStack<>), typeof(ConcurrentStackFormatter<>)},
#if !NET40
            {typeof(ReadOnlyDictionary<,>), typeof(ReadOnlyDictionaryFormatter<,>)},
            {typeof(IReadOnlyDictionary<,>), typeof(InterfaceReadOnlyDictionaryFormatter<,>)},
#endif
            {typeof(System.Collections.Concurrent.ConcurrentDictionary<,>), typeof(ConcurrentDictionaryFormatter<,>)},
            {typeof(Lazy<>), typeof(LazyFormatter<>)},
#if !NET40
            {typeof(Task<>), typeof(TaskValueFormatter<>)},
#endif
            #endif
        };

        // Reduce IL2CPP code generate size(don't write long code in <T>)
        internal static object GetFormatter(Type t)
        {
            var ti = t.GetTypeInfo();

            if (t.IsArray)
            {
                var rank = t.GetArrayRank();
                if (rank == 1)
                {
                    if (t.GetElementType() == typeof(byte)) // byte[] is also supported in builtin formatter.
                    {
                        return ByteArrayFormatter.Default;
                    }

                    return ActivatorUtils.FastCreateInstance(typeof(ArrayFormatter<>).GetCachedGenericType(t.GetElementType()));
                }
                else if (rank == 2)
                {
                    return ActivatorUtils.FastCreateInstance(typeof(TwoDimentionalArrayFormatter<>).GetCachedGenericType(t.GetElementType()));
                }
                else if (rank == 3)
                {
                    return ActivatorUtils.FastCreateInstance(typeof(ThreeDimentionalArrayFormatter<>).GetCachedGenericType(t.GetElementType()));
                }
                else if (rank == 4)
                {
                    return ActivatorUtils.FastCreateInstance(typeof(FourDimentionalArrayFormatter<>).GetCachedGenericType(t.GetElementType()));
                }
                else
                {
                    return null; // not supported built-in
                }
            }
            else if (ti.IsGenericType)
            {
                var genericType = ti.GetGenericTypeDefinition();
                var genericTypeInfo = genericType.GetTypeInfo();
                var isNullable = genericTypeInfo.IsNullable();
                var nullableElementType = isNullable ? ti.GenericTypeArguments[0] : null;

                const string _systemTupleType = "System.Tuple";
                const string _systemValueTupleType = "System.ValueTuple";

                if (genericType == typeof(KeyValuePair<,>))
                {
                    return CreateInstance(typeof(KeyValuePairFormatter<,>), ti.GenericTypeArguments);
                }
                else if (isNullable && nullableElementType.GetTypeInfo().IsConstructedGenericType() && nullableElementType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                {
                    return CreateInstance(typeof(NullableFormatter<>), new[] { nullableElementType });
                }

#if NETSTANDARD || DESKTOPCLR

#if !NET40
                // ValueTask
                else if (genericType == typeof(ValueTask<>))
                {
                    return CreateInstance(typeof(ValueTaskFormatter<>), ti.GenericTypeArguments);
                }
                else if (isNullable && nullableElementType.IsConstructedGenericType && nullableElementType.GetGenericTypeDefinition() == typeof(ValueTask<>))
                {
                    return CreateInstance(typeof(NullableFormatter<>), new[] { nullableElementType });
                }
#endif

                // Tuple
#if NET40
                else if (ti.AsType().FullName.StartsWith(_systemTupleType, StringComparison.Ordinal))
#else
                else if (ti.FullName.StartsWith(_systemTupleType, StringComparison.Ordinal))
#endif
                {
                    Type tupleFormatterType = null;
                    switch (ti.GenericTypeArguments.Length)
                    {
                        case 1:
                            tupleFormatterType = typeof(TupleFormatter<>);
                            break;
                        case 2:
                            tupleFormatterType = typeof(TupleFormatter<,>);
                            break;
                        case 3:
                            tupleFormatterType = typeof(TupleFormatter<,,>);
                            break;
                        case 4:
                            tupleFormatterType = typeof(TupleFormatter<,,,>);
                            break;
                        case 5:
                            tupleFormatterType = typeof(TupleFormatter<,,,,>);
                            break;
                        case 6:
                            tupleFormatterType = typeof(TupleFormatter<,,,,,>);
                            break;
                        case 7:
                            tupleFormatterType = typeof(TupleFormatter<,,,,,,>);
                            break;
                        case 8:
                            tupleFormatterType = typeof(TupleFormatter<,,,,,,,>);
                            break;
                        default:
                            break;
                    }

                    return CreateInstance(tupleFormatterType, ti.GenericTypeArguments);
                }

                // ValueTuple
#if NET40
                else if (ti.AsType().FullName.StartsWith(_systemValueTupleType, StringComparison.Ordinal))
#else
                else if (ti.FullName.StartsWith(_systemValueTupleType, StringComparison.Ordinal))
#endif
                {
                    Type tupleFormatterType = null;
                    switch (ti.GenericTypeArguments.Length)
                    {
                        case 1:
                            tupleFormatterType = typeof(ValueTupleFormatter<>);
                            break;
                        case 2:
                            tupleFormatterType = typeof(ValueTupleFormatter<,>);
                            break;
                        case 3:
                            tupleFormatterType = typeof(ValueTupleFormatter<,,>);
                            break;
                        case 4:
                            tupleFormatterType = typeof(ValueTupleFormatter<,,,>);
                            break;
                        case 5:
                            tupleFormatterType = typeof(ValueTupleFormatter<,,,,>);
                            break;
                        case 6:
                            tupleFormatterType = typeof(ValueTupleFormatter<,,,,,>);
                            break;
                        case 7:
                            tupleFormatterType = typeof(ValueTupleFormatter<,,,,,,>);
                            break;
                        case 8:
                            tupleFormatterType = typeof(ValueTupleFormatter<,,,,,,,>);
                            break;
                        default:
                            break;
                    }

                    return CreateInstance(tupleFormatterType, ti.GenericTypeArguments);
                }

#endif

                // ArraySegement
                else if (genericType == typeof(ArraySegment<>))
                {
                    if (ti.GenericTypeArguments[0] == typeof(byte))
                    {
                        return ByteArraySegmentFormatter.Default;
                    }
                    else
                    {
                        return CreateInstance(typeof(ArraySegmentFormatter<>), ti.GenericTypeArguments);
                    }
                }
                else if (isNullable && nullableElementType.GetTypeInfo().IsConstructedGenericType() && nullableElementType.GetGenericTypeDefinition() == typeof(ArraySegment<>))
                {
                    if (nullableElementType == typeof(ArraySegment<byte>))
                    {
                        return new StaticNullableFormatter<ArraySegment<byte>>(ByteArraySegmentFormatter.Default);
                    }
                    else
                    {
                        return CreateInstance(typeof(NullableFormatter<>), new[] { nullableElementType });
                    }
                }

                // Mapped formatter
                else
                {
                    Type formatterType;
                    if (formatterMap.TryGetValue(genericType, out formatterType))
                    {
                        return CreateInstance(formatterType, ti.GenericTypeArguments);
                    }

                    // generic collection
                    else if (ti.GenericTypeArguments.Length == 1
                          && ti.ImplementedInterfaces.Any(x => x.GetTypeInfo().IsConstructedGenericType() && x.GetGenericTypeDefinition() == typeof(ICollection<>))
                          && ti.DeclaredConstructors.Any(x => x.GetParameters().Length == 0))
                    {
                        var elemType = ti.GenericTypeArguments[0];
                        return CreateInstance(typeof(GenericCollectionFormatter<,>), new[] { elemType, t });
                    }
                    // generic dictionary
                    else if (ti.GenericTypeArguments.Length == 2
                          && ti.ImplementedInterfaces.Any(x => x.GetTypeInfo().IsConstructedGenericType() && x.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                          && ti.DeclaredConstructors.Any(x => x.GetParameters().Length == 0))
                    {
                        var keyType = ti.GenericTypeArguments[0];
                        var valueType = ti.GenericTypeArguments[1];
                        return CreateInstance(typeof(GenericDictionaryFormatter<,,>), new[] { keyType, valueType, t });
                    }
                }
            }
            else
            {
                // NonGeneric Collection
                if (t == typeof(IEnumerable))
                {
                    return NonGenericInterfaceEnumerableFormatter.Default;
                }
                else if (t == typeof(ICollection))
                {
                    return NonGenericInterfaceCollectionFormatter.Default;
                }
                else if (t == typeof(IList))
                {
                    return NonGenericInterfaceListFormatter.Default;
                }
                else if (t == typeof(IDictionary))
                {
                    return NonGenericInterfaceDictionaryFormatter.Default;
                }
                if (typeof(IList).GetTypeInfo().IsAssignableFrom(ti) && ti.DeclaredConstructors.Any(x => x.GetParameters().Length == 0))
                {
                    return ActivatorUtils.FastCreateInstance(typeof(NonGenericListFormatter<>).GetCachedGenericType(t));
                }
                else if (typeof(IDictionary).GetTypeInfo().IsAssignableFrom(ti) && ti.DeclaredConstructors.Any(x => x.GetParameters().Length == 0))
                {
                    return ActivatorUtils.FastCreateInstance(typeof(NonGenericDictionaryFormatter<>).GetCachedGenericType(t));
                }
            }

            return null;
        }

        static object CreateInstance(Type genericType, Type[] genericTypeArguments, params object[] arguments)
        {
            return ActivatorUtil.CreateInstance(genericType.GetCachedGenericType(genericTypeArguments), arguments);
        }
    }
}

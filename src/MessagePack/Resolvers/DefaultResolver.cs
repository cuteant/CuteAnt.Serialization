﻿using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using CuteAnt;
using MessagePack.Formatters;

namespace MessagePack.Resolvers
{
    public class DefaultResolver : FormatterResolver
    {
        public static readonly DefaultResolver Instance = new DefaultResolver();

        public DefaultResolver() { }

        public sealed override IMessagePackFormatter<T> GetFormatter<T>()
        {
            return DefaultResolverFormatterCache<T>.Formatter;
        }
    }

    internal static class DefaultResolverFormatterCache<T>
    {
        public static readonly IMessagePackFormatter<T> Formatter;

        static DefaultResolverFormatterCache()
        {
            if (typeof(T) == TypeConstants.ObjectType)
            {
                // final fallback
                Formatter = (IMessagePackFormatter<T>)DefaultResolverCore.ObjectFallbackFormatter;
            }
            else
            {
                Formatter = DefaultResolverCore.Instance.GetFormatter<T>();
            }
        }
    }

    internal sealed class DefaultResolverCore : FormatterResolver
    {
        public static readonly IFormatterResolver Instance;

        private static readonly IFormatterResolver[] s_defaultResolvers;

        private const int Locked = 1;
        private const int Unlocked = 0;
        private static int s_isFreezed = Unlocked;
        private static List<IMessagePackFormatter> s_formatters;
        private static List<IFormatterResolver> s_resolvers;

        static DefaultResolverCore()
        {
            s_defaultResolvers = new IFormatterResolver[]
            {
                //UnsafeBinaryResolver.Instance,
                NativeDateTimeResolver.Instance, // Native c# DateTime format, preserving timezone

                BuiltinResolver.Instance, // Try Builtin
                AttributeFormatterResolver.Instance, // Try use [MessagePackFormatter]

                DynamicEnumResolver.Instance, // Try Enum
                DynamicGenericResolver.Instance, // Try Array, Tuple, Collection
                DynamicUnionResolver.Instance, // Try Union(Interface)

                DynamicObjectResolverAllowPrivate.Instance, // Try Object
                DynamicContractlessObjectResolverAllowPrivate.Instance, // Serializes keys as strings
            };

            s_formatters = new List<IMessagePackFormatter>();
            s_resolvers = s_defaultResolvers.ToList();

            Instance = new DefaultResolverCore();
        }

        DefaultResolverCore() { }

        private static IMessagePackFormatter<object> s_objectFallbackFormatter;
        public static IMessagePackFormatter<object> ObjectFallbackFormatter
        {
            [MethodImpl(InlineMethod.Value)]
            get { return Volatile.Read(ref s_objectFallbackFormatter) ?? EnsureObjectFallbackFormatterCreated(); }
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static IMessagePackFormatter<object> EnsureObjectFallbackFormatterCreated()
        {
            Interlocked.CompareExchange(ref s_objectFallbackFormatter, new DynamicObjectTypeFallbackFormatter(DefaultResolverCore.Instance), null);
            return s_objectFallbackFormatter;
        }

        public static bool TryRegister(params IFormatterResolver[] resolvers)
        {
            if (null == resolvers || resolvers.Length == 0) { return false; }
            if (Locked == Volatile.Read(ref s_isFreezed)) { return false; }

            List<IFormatterResolver> snapshot, newCache;
            do
            {
                snapshot = Volatile.Read(ref s_resolvers);
                newCache = new List<IFormatterResolver>();
                newCache.AddRange(resolvers);
                if (snapshot.Count > 0) { newCache.AddRange(snapshot); }
            } while (!ReferenceEquals(
                Interlocked.CompareExchange(ref s_resolvers, newCache, snapshot), snapshot));
            return true;
        }

        public static void Register(params IFormatterResolver[] resolvers)
        {
            if (null == resolvers || resolvers.Length == 0) { return; }

            if (TryRegister(resolvers)) { return; }
            ThrowHelper.ThrowInvalidOperationException(ExceptionResource.MessagePack_Register_Err);
        }

        public static bool TryRegister(params IMessagePackFormatter[] formatters)
        {
            if (null == formatters || formatters.Length == 0) { return false; }
            if (Locked == Volatile.Read(ref s_isFreezed)) { return false; }

            List<IMessagePackFormatter> snapshot, newCache;
            do
            {
                snapshot = Volatile.Read(ref s_formatters);
                newCache = new List<IMessagePackFormatter>();
                newCache.AddRange(formatters);
                if (snapshot.Count > 0) { newCache.AddRange(snapshot); }
            } while (!ReferenceEquals(
                Interlocked.CompareExchange(ref s_formatters, newCache, snapshot), snapshot));
            return true;
        }

        public static void Register(params IMessagePackFormatter[] formatters)
        {
            if (null == formatters || formatters.Length == 0) { return; }

            if (TryRegister(formatters)) { return; }
            ThrowHelper.ThrowInvalidOperationException(ExceptionResource.MessagePack_Register_Err);
        }

        public static void Register(IMessagePackFormatter[] formatters, IFormatterResolver[] resolvers)
        {
            Register(formatters);
            Register(resolvers);
        }

        public static bool TryRegister(IMessagePackFormatter[] formatters, IFormatterResolver[] resolvers)
        {
            if (!TryRegister(formatters)) { return false; }
            if (!TryRegister(resolvers)) { return false; }
            return true;
        }

        public override IMessagePackFormatter<T> GetFormatter<T>()
        {
            return FormatterCache<T>.Formatter;
        }

        static class FormatterCache<T>
        {
            public static readonly IMessagePackFormatter<T> Formatter;

            static FormatterCache()
            {
                Interlocked.CompareExchange(ref s_isFreezed, Locked, Unlocked);

                var formatters = Volatile.Read(ref s_formatters);
                foreach (var item in formatters)
                {
                    foreach (var implInterface in item.GetType().GetTypeInfo().ImplementedInterfaces)
                    {
#if NET40
                        if (implInterface.IsGenericType && implInterface.GenericTypeArguments()[0] == typeof(T))
#else
                        if (implInterface.IsGenericType && implInterface.GenericTypeArguments[0] == typeof(T))
#endif
                        {
                            Formatter = (IMessagePackFormatter<T>)item;
                            return;
                        }
                    }
                }

                var resolvers = Volatile.Read(ref s_resolvers);
                foreach (var item in resolvers)
                {
                    var f = item.GetFormatter<T>();
                    if (f != null)
                    {
                        Formatter = f;
                        return;
                    }
                }
            }
        }
    }
}

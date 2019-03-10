﻿using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using Utf8Json.Formatters;
using Utf8Json.Resolvers;

namespace Utf8Json
{
    public static class Utf8JsonStandardResolver
    {
        /// <summary>AllowPrivate:True,  ExcludeNull:True,  NameMutate:Original</summary>
        public static readonly IJsonFormatterResolver Default = AllowPrivateExcludeNullResolver.Instance;
        /// <summary>AllowPrivate:True,  ExcludeNull:True,  NameMutate:CamelCase</summary>
        public static readonly IJsonFormatterResolver CamelCase = AllowPrivateExcludeNullCamelCaseResolver.Instance;
        /// <summary>AllowPrivate:True,  ExcludeNull:True,  NameMutate:SnakeCase</summary>
        public static readonly IJsonFormatterResolver SnakeCase = AllowPrivateExcludeNullSnakeCaseResolver.Instance;

        public static void Register(params IJsonFormatterResolver[] resolvers)
        {
            AllowPrivateExcludeNullStandardResolverCore.Register(resolvers);
            AllowPrivateExcludeNullCamelCaseStandardResolverCore.Register(resolvers);
            AllowPrivateExcludeNullSnakeCaseStandardResolverCore.Register(resolvers);
        }

        public static void Register(params IJsonFormatter[] formatters)
        {
            AllowPrivateExcludeNullStandardResolverCore.Register(formatters);
            AllowPrivateExcludeNullCamelCaseStandardResolverCore.Register(formatters);
            AllowPrivateExcludeNullSnakeCaseStandardResolverCore.Register(formatters);
        }

        public static void Register(IJsonFormatter[] formatters, IJsonFormatterResolver[] resolvers)
        {
            AllowPrivateExcludeNullStandardResolverCore.Register(formatters, resolvers);
            AllowPrivateExcludeNullCamelCaseStandardResolverCore.Register(formatters, resolvers);
            AllowPrivateExcludeNullSnakeCaseStandardResolverCore.Register(formatters, resolvers);
        }

        public static bool TryRegister(params IJsonFormatterResolver[] resolvers)
        {
            if (!AllowPrivateExcludeNullStandardResolverCore.TryRegister(resolvers)) { return false; }
            if (!AllowPrivateExcludeNullCamelCaseStandardResolverCore.TryRegister(resolvers)) { return false; }
            if (!AllowPrivateExcludeNullSnakeCaseStandardResolverCore.TryRegister(resolvers)) { return false; }
            return true;
        }

        public static bool TryRegister(params IJsonFormatter[] formatters)
        {
            if (!AllowPrivateExcludeNullStandardResolverCore.TryRegister(formatters)) { return false; }
            if (!AllowPrivateExcludeNullCamelCaseStandardResolverCore.TryRegister(formatters)) { return false; }
            if (!AllowPrivateExcludeNullSnakeCaseStandardResolverCore.TryRegister(formatters)) { return false; }
            return true;
        }

        public static bool TryRegister(IJsonFormatter[] formatters, IJsonFormatterResolver[] resolvers)
        {
            if (!AllowPrivateExcludeNullStandardResolverCore.TryRegister(formatters, resolvers)) { return false; }
            if (!AllowPrivateExcludeNullCamelCaseStandardResolverCore.TryRegister(formatters, resolvers)) { return false; }
            if (!AllowPrivateExcludeNullSnakeCaseStandardResolverCore.TryRegister(formatters, resolvers)) { return false; }
            return true;
        }
    }

    internal static class DefaultResolverHelper
    {
        internal static readonly IJsonFormatterResolver[] CompositeResolverBase = new[]
        {
            BuiltinResolver.Instance, // Builtin
            EnumResolver.Default,     // Enum(default => string)
            DynamicGenericResolver.Instance, // T[], List<T>, etc...
            AttributeFormatterResolver.Instance // [JsonFormatter]
        };
    }

    internal sealed class AllowPrivateExcludeNullResolver : IJsonFormatterResolver
    {
        // configure
        public static readonly IJsonFormatterResolver Instance = new AllowPrivateExcludeNullResolver();

        private static IJsonFormatter<object> s_objectFallbackFormatter;
        public static IJsonFormatter<object> ObjectFallbackFormatter
        {
            [MethodImpl(InlineMethod.Value)]
            get { return Volatile.Read(ref s_objectFallbackFormatter) ?? EnsureObjectFallbackFormatterCreated(); }
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static IJsonFormatter<object> EnsureObjectFallbackFormatterCreated()
        {
            Interlocked.CompareExchange(ref s_objectFallbackFormatter, new DynamicObjectTypeFallbackFormatter(AllowPrivateExcludeNullStandardResolverCore.Instance), null);
            return s_objectFallbackFormatter;
        }

        AllowPrivateExcludeNullResolver()
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
                if (typeof(T) == typeof(object))
                {
                    formatter = (IJsonFormatter<T>)ObjectFallbackFormatter;
                }
                else
                {
                    formatter = AllowPrivateExcludeNullStandardResolverCore.Instance.GetFormatter<T>();
                }
            }
        }
    }
    internal sealed class AllowPrivateExcludeNullStandardResolverCore : IJsonFormatterResolver
    {
        public static readonly IJsonFormatterResolver Instance = new AllowPrivateExcludeNullStandardResolverCore();

        static readonly IJsonFormatterResolver[] s_defaultResolvers;
        private const int Locked = 1;
        private const int Unlocked = 0;
        private static int s_isFreezed = Unlocked;
        private static List<IJsonFormatter> s_formatters;
        private static List<IJsonFormatterResolver> s_resolvers;

        static AllowPrivateExcludeNullStandardResolverCore()
        {
            s_defaultResolvers = DefaultResolverHelper.CompositeResolverBase.Concat(new[] { DynamicObjectResolver.AllowPrivateExcludeNull }).ToArray();
            s_formatters = new List<IJsonFormatter>();
            s_resolvers = s_defaultResolvers.ToList();
        }
        AllowPrivateExcludeNullStandardResolverCore()
        {
        }

        public static bool TryRegister(params IJsonFormatterResolver[] resolvers)
        {
            if (null == resolvers || resolvers.Length == 0) { return false; }
            if (Locked == Volatile.Read(ref s_isFreezed)) { return false; }

            List<IJsonFormatterResolver> snapshot, newCache;
            do
            {
                snapshot = Volatile.Read(ref s_resolvers);
                newCache = new List<IJsonFormatterResolver>();
                newCache.AddRange(resolvers);
                if (snapshot.Count > 0) { newCache.AddRange(snapshot); }
            } while (!ReferenceEquals(
                Interlocked.CompareExchange(ref s_resolvers, newCache, snapshot), snapshot));
            return true;
        }

        public static void Register(params IJsonFormatterResolver[] resolvers)
        {
            if (null == resolvers || resolvers.Length == 0) { return; }

            if (TryRegister(resolvers)) { return; }
            ThrowHelper.ThrowInvalidOperationException(ExceptionResource.Utf8Json_Register_Err);
        }

        public static bool TryRegister(params IJsonFormatter[] formatters)
        {
            if (null == formatters || formatters.Length == 0) { return false; }
            if (Locked == Volatile.Read(ref s_isFreezed)) { return false; }

            List<IJsonFormatter> snapshot, newCache;
            do
            {
                snapshot = Volatile.Read(ref s_formatters);
                newCache = new List<IJsonFormatter>();
                newCache.AddRange(formatters);
                if (snapshot.Count > 0) { newCache.AddRange(snapshot); }
            } while (!ReferenceEquals(
                Interlocked.CompareExchange(ref s_formatters, newCache, snapshot), snapshot));
            return true;
        }

        public static void Register(params IJsonFormatter[] formatters)
        {
            if (null == formatters || formatters.Length == 0) { return; }

            if (TryRegister(formatters)) { return; }
            ThrowHelper.ThrowInvalidOperationException(ExceptionResource.Utf8Json_Register_Err);
        }

        public static void Register(IJsonFormatter[] formatters, IJsonFormatterResolver[] resolvers)
        {
            Register(formatters);
            Register(resolvers);
        }

        public static bool TryRegister(IJsonFormatter[] formatters, IJsonFormatterResolver[] resolvers)
        {
            if (!TryRegister(formatters)) { return false; }
            if (!TryRegister(resolvers)) { return false; }
            return true;
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
                            formatter = (IJsonFormatter<T>)item;
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
                        formatter = f;
                        return;
                    }
                }
            }
        }
    }

    internal sealed class AllowPrivateExcludeNullCamelCaseResolver : IJsonFormatterResolver
    {
        // configure
        public static readonly IJsonFormatterResolver Instance = new AllowPrivateExcludeNullCamelCaseResolver();

        private static IJsonFormatter<object> s_objectFallbackFormatter;
        public static IJsonFormatter<object> ObjectFallbackFormatter
        {
            [MethodImpl(InlineMethod.Value)]
            get { return Volatile.Read(ref s_objectFallbackFormatter) ?? EnsureObjectFallbackFormatterCreated(); }
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static IJsonFormatter<object> EnsureObjectFallbackFormatterCreated()
        {
            Interlocked.CompareExchange(ref s_objectFallbackFormatter, new DynamicObjectTypeFallbackFormatter(AllowPrivateExcludeNullCamelCaseStandardResolverCore.Instance), null);
            return s_objectFallbackFormatter;
        }

        AllowPrivateExcludeNullCamelCaseResolver()
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
                if (typeof(T) == typeof(object))
                {
                    formatter = (IJsonFormatter<T>)ObjectFallbackFormatter;
                }
                else
                {
                    formatter = AllowPrivateExcludeNullCamelCaseStandardResolverCore.Instance.GetFormatter<T>();
                }
            }
        }
    }
    internal sealed class AllowPrivateExcludeNullCamelCaseStandardResolverCore : IJsonFormatterResolver
    {
        public static readonly IJsonFormatterResolver Instance = new AllowPrivateExcludeNullCamelCaseStandardResolverCore();

        private static readonly IJsonFormatterResolver[] s_defaultResolvers;
        private const int Locked = 1;
        private const int Unlocked = 0;
        private static int s_isFreezed = Unlocked;
        private static List<IJsonFormatter> s_formatters;
        private static List<IJsonFormatterResolver> s_resolvers;

        static AllowPrivateExcludeNullCamelCaseStandardResolverCore()
        {
            s_defaultResolvers = DefaultResolverHelper.CompositeResolverBase.Concat(new[] { DynamicObjectResolver.AllowPrivateExcludeNullCamelCase }).ToArray();
            s_resolvers = s_defaultResolvers.ToList();
            s_formatters = new List<IJsonFormatter>();
        }
        AllowPrivateExcludeNullCamelCaseStandardResolverCore()
        {
        }

        public static bool TryRegister(params IJsonFormatterResolver[] resolvers)
        {
            if (null == resolvers || resolvers.Length == 0) { return false; }
            if (Locked == Volatile.Read(ref s_isFreezed)) { return false; }

            List<IJsonFormatterResolver> snapshot, newCache;
            do
            {
                snapshot = Volatile.Read(ref s_resolvers);
                newCache = new List<IJsonFormatterResolver>();
                newCache.AddRange(resolvers);
                if (snapshot.Count > 0) { newCache.AddRange(snapshot); }
            } while (!ReferenceEquals(
                Interlocked.CompareExchange(ref s_resolvers, newCache, snapshot), snapshot));
            return true;
        }

        public static void Register(params IJsonFormatterResolver[] resolvers)
        {
            if (null == resolvers || resolvers.Length == 0) { return; }

            if (TryRegister(resolvers)) { return; }
            ThrowHelper.ThrowInvalidOperationException(ExceptionResource.Utf8Json_Register_Err);
        }

        public static bool TryRegister(params IJsonFormatter[] formatters)
        {
            if (null == formatters || formatters.Length == 0) { return false; }
            if (Locked == Volatile.Read(ref s_isFreezed)) { return false; }

            List<IJsonFormatter> snapshot, newCache;
            do
            {
                snapshot = Volatile.Read(ref s_formatters);
                newCache = new List<IJsonFormatter>();
                newCache.AddRange(formatters);
                if (snapshot.Count > 0) { newCache.AddRange(snapshot); }
            } while (!ReferenceEquals(
                Interlocked.CompareExchange(ref s_formatters, newCache, snapshot), snapshot));
            return true;
        }

        public static void Register(params IJsonFormatter[] formatters)
        {
            if (null == formatters || formatters.Length == 0) { return; }

            if (TryRegister(formatters)) { return; }
            ThrowHelper.ThrowInvalidOperationException(ExceptionResource.Utf8Json_Register_Err);
        }

        public static void Register(IJsonFormatter[] formatters, IJsonFormatterResolver[] resolvers)
        {
            Register(formatters);
            Register(resolvers);
        }

        public static bool TryRegister(IJsonFormatter[] formatters, IJsonFormatterResolver[] resolvers)
        {
            if (!TryRegister(formatters)) { return false; }
            if (!TryRegister(resolvers)) { return false; }
            return true;
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
                            formatter = (IJsonFormatter<T>)item;
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
                        formatter = f;
                        return;
                    }
                }
            }
        }
    }

    internal sealed class AllowPrivateExcludeNullSnakeCaseResolver : IJsonFormatterResolver
    {
        // configure
        public static readonly IJsonFormatterResolver Instance = new AllowPrivateExcludeNullSnakeCaseResolver();

        private static IJsonFormatter<object> s_objectFallbackFormatter;
        public static IJsonFormatter<object> ObjectFallbackFormatter
        {
            [MethodImpl(InlineMethod.Value)]
            get { return Volatile.Read(ref s_objectFallbackFormatter) ?? EnsureObjectFallbackFormatterCreated(); }
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static IJsonFormatter<object> EnsureObjectFallbackFormatterCreated()
        {
            Interlocked.CompareExchange(ref s_objectFallbackFormatter, new DynamicObjectTypeFallbackFormatter(AllowPrivateExcludeNullSnakeCaseStandardResolverCore.Instance), null);
            return s_objectFallbackFormatter;
        }

        AllowPrivateExcludeNullSnakeCaseResolver()
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
                if (typeof(T) == typeof(object))
                {
                    formatter = (IJsonFormatter<T>)ObjectFallbackFormatter;
                }
                else
                {
                    formatter = AllowPrivateExcludeNullSnakeCaseStandardResolverCore.Instance.GetFormatter<T>();
                }
            }
        }
    }
    internal sealed class AllowPrivateExcludeNullSnakeCaseStandardResolverCore : IJsonFormatterResolver
    {
        public static readonly IJsonFormatterResolver Instance = new AllowPrivateExcludeNullSnakeCaseStandardResolverCore();

        private static readonly IJsonFormatterResolver[] s_defaultResolvers;
        private const int Locked = 1;
        private const int Unlocked = 0;
        private static int s_isFreezed = Unlocked;
        private static List<IJsonFormatter> s_formatters;
        private static List<IJsonFormatterResolver> s_resolvers;

        static AllowPrivateExcludeNullSnakeCaseStandardResolverCore()
        {
            s_defaultResolvers = DefaultResolverHelper.CompositeResolverBase.Concat(new[] { DynamicObjectResolver.AllowPrivateExcludeNullSnakeCase }).ToArray();
            s_resolvers = s_defaultResolvers.ToList();
            s_formatters = new List<IJsonFormatter>();
        }
        AllowPrivateExcludeNullSnakeCaseStandardResolverCore()
        {
        }

        public static bool TryRegister(params IJsonFormatterResolver[] resolvers)
        {
            if (null == resolvers || resolvers.Length == 0) { return false; }
            if (Locked == Volatile.Read(ref s_isFreezed)) { return false; }

            List<IJsonFormatterResolver> snapshot, newCache;
            do
            {
                snapshot = Volatile.Read(ref s_resolvers);
                newCache = new List<IJsonFormatterResolver>();
                newCache.AddRange(resolvers);
                if (snapshot.Count > 0) { newCache.AddRange(snapshot); }
            } while (!ReferenceEquals(
                Interlocked.CompareExchange(ref s_resolvers, newCache, snapshot), snapshot));
            return true;
        }

        public static void Register(params IJsonFormatterResolver[] resolvers)
        {
            if (null == resolvers || resolvers.Length == 0) { return; }

            if (TryRegister(resolvers)) { return; }
            ThrowHelper.ThrowInvalidOperationException(ExceptionResource.Utf8Json_Register_Err);
        }

        public static bool TryRegister(params IJsonFormatter[] formatters)
        {
            if (null == formatters || formatters.Length == 0) { return false; }
            if (Locked == Volatile.Read(ref s_isFreezed)) { return false; }

            List<IJsonFormatter> snapshot, newCache;
            do
            {
                snapshot = Volatile.Read(ref s_formatters);
                newCache = new List<IJsonFormatter>();
                newCache.AddRange(formatters);
                if (snapshot.Count > 0) { newCache.AddRange(snapshot); }
            } while (!ReferenceEquals(
                Interlocked.CompareExchange(ref s_formatters, newCache, snapshot), snapshot));
            return true;
        }

        public static void Register(params IJsonFormatter[] formatters)
        {
            if (null == formatters || formatters.Length == 0) { return; }

            if (TryRegister(formatters)) { return; }
            ThrowHelper.ThrowInvalidOperationException(ExceptionResource.Utf8Json_Register_Err);
        }

        public static void Register(IJsonFormatter[] formatters, IJsonFormatterResolver[] resolvers)
        {
            Register(formatters);
            Register(resolvers);
        }

        public static bool TryRegister(IJsonFormatter[] formatters, IJsonFormatterResolver[] resolvers)
        {
            if (!TryRegister(formatters)) { return false; }
            if (!TryRegister(resolvers)) { return false; }
            return true;
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
                            formatter = (IJsonFormatter<T>)item;
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
                        formatter = f;
                        return;
                    }
                }
            }
        }
    }
}

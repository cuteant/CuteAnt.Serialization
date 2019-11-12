﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using CuteAnt.Reflection;
using ServiceStack.Text;

namespace ServiceStack
{
    public class PropertyAccessor
    {
        public PropertyAccessor(
            PropertyInfo propertyInfo,
            MemberGetter publicGetter,
            MemberSetter publicSetter)
        {
            PropertyInfo = propertyInfo;
            PublicGetter = publicGetter;
            PublicSetter = publicSetter;
        }

        public PropertyInfo PropertyInfo { get; }

        public MemberGetter PublicGetter { get; }

        public MemberSetter PublicSetter { get; }
    }

    public class TypeProperties<T> : TypeProperties
    {
        public static readonly TypeProperties<T> Instance = new TypeProperties<T>();

        static TypeProperties()
        {
            Instance.Type = typeof(T);
            Instance.PublicPropertyInfos = typeof(T).GetPublicProperties();
            foreach (var pi in Instance.PublicPropertyInfos)
            {
                try
                {
                    Instance.PropertyMap[pi.Name] = new PropertyAccessor(
                        pi,
                        ReflectionOptimizer.Instance.CreateGetter(pi),
                        ReflectionOptimizer.Instance.CreateSetter(pi)
                    );
                }
                catch (Exception ex)
                {
                    Tracer.Instance.WriteError(ex);
                }
            }
        }

        public new static PropertyAccessor GetAccessor(string propertyName)
        {
            return Instance.PropertyMap.TryGetValue(propertyName, out PropertyAccessor info)
                ? info
                : null;
        }
    }

    public abstract class TypeProperties
    {
        static Dictionary<Type, TypeProperties> CacheMap = new Dictionary<Type, TypeProperties>();

        public static readonly Type FactoryType = typeof(TypeProperties<>);

        public static TypeProperties Get(Type type)
        {
            if (CacheMap.TryGetValue(type, out TypeProperties value))
                return value;

            var genericType = FactoryType.GetCachedGenericType(type);
            var instanceFi = genericType.GetPublicStaticField("Instance");
            var instance = (TypeProperties)instanceFi.GetValue(null);

            Dictionary<Type, TypeProperties> snapshot, newCache;
            do
            {
                snapshot = CacheMap;
                newCache = new Dictionary<Type, TypeProperties>(CacheMap)
                {
                    [type] = instance
                };
            } while (!ReferenceEquals(
                Interlocked.CompareExchange(ref CacheMap, newCache, snapshot), snapshot));

            return instance;
        }

        public PropertyAccessor GetAccessor(string propertyName)
        {
            return PropertyMap.TryGetValue(propertyName, out PropertyAccessor info)
                ? info
                : null;
        }

        public Type Type { get; protected set; }

        public readonly Dictionary<string, PropertyAccessor> PropertyMap =
            new Dictionary<string, PropertyAccessor>(PclExport.Instance.InvariantComparerIgnoreCase);

        public PropertyInfo[] PublicPropertyInfos { get; protected set; }

        public PropertyInfo GetPublicProperty(string name)
        {
            foreach (var pi in PublicPropertyInfos)
            {
                if (pi.Name == name)
                    return pi;
            }
            return null;
        }

        public MemberGetter GetPublicGetter(PropertyInfo pi) => GetPublicGetter(pi?.Name);

        public MemberGetter GetPublicGetter(string name)
        {
            if (name == null)
                return null;

            return PropertyMap.TryGetValue(name, out PropertyAccessor info)
                ? info.PublicGetter
                : null;
        }

        public MemberSetter GetPublicSetter(PropertyInfo pi) => GetPublicSetter(pi?.Name);

        public MemberSetter GetPublicSetter(string name)
        {
            if (name == null)
                return null;

            return PropertyMap.TryGetValue(name, out PropertyAccessor info)
                ? info.PublicSetter
                : null;
        }
    }

    public static class PropertyInvokerUtils
    {
        public static MemberGetter CreateGetter(this PropertyInfo propertyInfo) =>
            ReflectionOptimizer.Instance.CreateGetter(propertyInfo);

        public static MemberGetter<T> CreateGetter<T>(this PropertyInfo propertyInfo) =>
            ReflectionOptimizer.Instance.CreateGetter<T>(propertyInfo);

        public static MemberSetter CreateSetter(this PropertyInfo propertyInfo) =>
            ReflectionOptimizer.Instance.CreateSetter(propertyInfo);

        public static MemberSetter<T> CreateSetter<T>(this PropertyInfo propertyInfo) =>
            ReflectionOptimizer.Instance.CreateSetter<T>(propertyInfo);
    }
}


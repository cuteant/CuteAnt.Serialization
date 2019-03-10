﻿using System;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using CuteAnt.Reflection;
using Utf8Json.Internal;

namespace Utf8Json.Resolvers
{
    /// <summary>
    /// Get formatter from [JsonFormatter] attribute.
    /// </summary>
    public sealed class AttributeFormatterResolver : IJsonFormatterResolver
    {
        public static IJsonFormatterResolver Instance = new AttributeFormatterResolver();

        AttributeFormatterResolver()
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
#if (UNITY_METRO || UNITY_WSA) && !NETFX_CORE
                var attr = (JsonFormatterAttribute)typeof(T).GetCustomAttributes(typeof(JsonFormatterAttribute), true).FirstOrDefault();
#else
                var attr = typeof(T).GetCustomAttributeX<JsonFormatterAttribute>();
#endif
                if (attr == null)
                {
                    return;
                }

                try
                {
#if NET40
                    if (attr.FormatterType.IsGenericType && !attr.FormatterType.IsConstructedGenericType())
#else
                    if (attr.FormatterType.IsGenericType && !attr.FormatterType.IsConstructedGenericType)
#endif
                    {
                        var t = attr.FormatterType.GetCachedGenericType(typeof(T)); // use T self
                        formatter = (IJsonFormatter<T>)ActivatorUtil.CreateInstance(t, attr.Arguments);
                    }
                    else
                    {
                        formatter = (IJsonFormatter<T>)ActivatorUtil.CreateInstance(attr.FormatterType, attr.Arguments);
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("Can not create formatter from JsonFormatterAttribute, check the target formatter is public and has constructor with right argument. FormatterType:" + attr.FormatterType.Name, ex);
                }
            }
        }
    }
}

﻿using System;
using System.ComponentModel;

//using ProtoBuf.Meta;
#if FEAT_IKVM
using Type = IKVM.Reflection.Type;
using IKVM.Reflection;
#else
using System.Reflection;
#endif
namespace ProtoBuf
{
  /// <summary>
  /// Indicates the known-types to support for an individual
  /// message. This serializes each level in the hierarchy as
  /// a nested message to retain wire-compatibility with
  /// other protocol-buffer implementations.
  /// </summary>
  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true, Inherited = false)]
  public sealed class ProtoIncludeAttribute : Attribute
  {
    ///<summary>
    /// Creates a new instance of the ProtoIncludeAttribute.
    /// </summary>
    /// <param name="tag">The unique index (within the type) that will identify this data.</param>
    /// <param name="knownType">The additional type to serialize/deserialize.</param>
    public ProtoIncludeAttribute(int tag, System.Type knownType)
        : this(tag, knownType == null ? "" : knownType.AssemblyQualifiedName) { }

    /// <summary>
    /// Creates a new instance of the ProtoIncludeAttribute.
    /// </summary>
    /// <param name="tag">The unique index (within the type) that will identify this data.</param>
    /// <param name="knownTypeName">The additional type to serialize/deserialize.</param>
    public ProtoIncludeAttribute(int tag, string knownTypeName)
    {
      if (tag <= 0) throw new ArgumentOutOfRangeException("tag", "Tags must be positive integers");
      if (string.IsNullOrEmpty(knownTypeName)) throw new ArgumentNullException("knownTypeName", "Known type cannot be blank");
      this.tag = tag;
      this.knownTypeName = knownTypeName;
    }

    /// <summary>
    /// Gets the unique index (within the type) that will identify this data.
    /// </summary>
    public int Tag { get { return tag; } }
    private readonly int tag;

    /// <summary>
    /// Gets the additional type to serialize/deserialize.
    /// </summary>
    public string KnownTypeName { get { return knownTypeName; } }
    private readonly string knownTypeName;

    /// <summary>
    /// Gets the additional type to serialize/deserialize.
    /// </summary>
    public Type KnownType
    {
      get
      {
        //return TypeModel.ResolveKnownType(KnownTypeName, null, null);
        return ResolveKnownType(KnownTypeName, null);
      }
    }

    /// <summary>
    /// Specifies whether the inherited sype's sub-message should be
    /// written with a length-prefix (default), or with group markers.
    /// </summary>
    [DefaultValue(DataFormat.Default)]
    public DataFormat DataFormat
    {
      get { return dataFormat; }
      set { dataFormat = value; }
    }
    private DataFormat dataFormat = DataFormat.Default;

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    internal static Type ResolveKnownType(string name, Assembly assembly)
    {
      if (string.IsNullOrEmpty(name)) return null;
      try
      {
#if FEAT_IKVM
        // looks like a NullReferenceException, but this should call into RuntimeTypeModel's version
        Type type = model == null ? null : model.GetType(name, assembly);
#else
        Type type = Type.GetType(name);
#endif
        if (type != null) return type;
      }
      catch { }
      try
      {
        int i = name.IndexOf(',');
        string fullName = (i > 0 ? name.Substring(0, i) : name).Trim();
//#if !(WINRT || FEAT_IKVM || COREFX)
        if (assembly == null) assembly = Assembly.GetCallingAssembly();
//#endif
        Type type = assembly == null ? null : assembly.GetType(fullName);
        if (type != null) return type;
      }
      catch { }
      return null;
    }
  }
}

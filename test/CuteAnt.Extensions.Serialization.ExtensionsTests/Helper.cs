﻿using System;
using System.Collections.Generic;
using System.Linq;
using MessagePack;
using ProtoBuf;
using Xunit;

namespace CuteAnt.Extensions.Serialization.Tests
{
  internal class Helper
  {
    public static void ComparePoco(SerializerPocoSerializable expected, SerializerPocoSerializable actual)
    {
      Assert.Equal(expected.StringProperty, actual.StringProperty);
      Assert.Equal(expected.IntProperty, actual.IntProperty);
      Assert.Equal(expected.StringArrayProperty, actual.StringArrayProperty);
      Assert.Equal(expected.StringListProperty, actual.StringListProperty);
      Assert.Equal(expected.ChildDictionaryProperty.Keys.ToArray(), actual.ChildDictionaryProperty.Keys.ToArray());
      Assert.Equal((expected.ChildDictionaryProperty.Values.ToArray())[0].StringProperty, (actual.ChildDictionaryProperty.Values.ToArray())[0].StringProperty);
    }
  }

  [Serializable]
  [ProtoContract]
  [MessagePackObject]
  public class SerializerPocoSerializable
  {
    [ProtoMember(1)]
    [Key(0)]
    public string StringProperty { get; set; }

    [ProtoMember(2)]
    [Key(1)]
    public int IntProperty { get; set; }

    [ProtoMember(3)]
    [Key(2)]
    public string[] StringArrayProperty { get; set; }

    [ProtoMember(4)]
    [Key(3)]
    public List<string> StringListProperty { get; set; }

    [ProtoMember(5)]
    [Key(4)]
    public Dictionary<string, ChildPocco> ChildDictionaryProperty { get; set; }

    public static SerializerPocoSerializable Create()
    {
      return new SerializerPocoSerializable()
      {
        StringProperty = "a",
        IntProperty = 2018,
        StringArrayProperty = new string[] { "foo", "bar" },
        StringListProperty = new List<string>(new string[] { "风雨送春归", "飞雪迎春到", "已是悬崖百丈冰", "犹有花枝俏", "俏也不争春", "只把春来报", "待到山花烂漫时", "她在丛中笑" }),
        ChildDictionaryProperty = new Dictionary<string, ChildPocco>()
        {
          { "king", new ChildPocco() { StringProperty = "good" } },
        }
      };
    }
  }

  [Serializable]
  [ProtoContract]
  [MessagePackObject]
  public class ChildPocco
  {
    [ProtoMember(1)]
    [Key(0)]
    public string StringProperty { get; set; }
  }

  [MessagePack.Union(0, typeof(FooClass))]
  [MessagePack.Union(1, typeof(BarClass))]
  public interface IUnionSample
  {
  }

  [MessagePackObject]
  public class FooClass : IUnionSample
  {
    [Key(0)]
    public int XYZ { get; set; }
  }

  [MessagePackObject]
  public class BarClass : IUnionSample
  {
    [Key(0)]
    public string OPQ { get; set; }
  }

  [Union(0, typeof(SubUnionType1))]
  [Union(1, typeof(SubUnionType2))]
  [MessagePackObject]
  public abstract class ParentUnionType
  {
    [Key(0)]
    public Guid MyProperty { get; set; }
  }

  [MessagePackObject]
  public class SubUnionType1 : ParentUnionType
  {
    [Key(1)]
    public int MyProperty1 { get; set; }
  }

  [MessagePackObject]
  public class SubUnionType2 : ParentUnionType
  {
    [Key(1)]
    public int MyProperty2 { get; set; }
  }
}

﻿#region copyright
// -----------------------------------------------------------------------
//  <copyright file="StringEx.cs" company="Akka.NET Team">
//      Copyright (C) 2015-2016 AsynkronIT <https://github.com/AsynkronIT>
//      Copyright (C) 2016-2016 Akka.NET Team <https://github.com/akkadotnet>
//  </copyright>
// -----------------------------------------------------------------------
#endregion

using System.Runtime.CompilerServices;
using CuteAnt;

namespace Hyperion.Extensions
{
  internal static class StringEx
  {
    [MethodImpl(InlineMethod.Value)]
    internal static byte[] ToUtf8Bytes(this string str)
    {
      return NoAllocBitConverter.Utf8.GetBytes(str);
    }

    [MethodImpl(InlineMethod.Value)]
    internal static string FromUtf8Bytes(byte[] bytes, int offset, int count)
    {
      return NoAllocBitConverter.Utf8.GetString(bytes, offset, count);
    }
  }
}
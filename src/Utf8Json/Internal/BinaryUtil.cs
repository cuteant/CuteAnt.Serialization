﻿using System;
using System.Collections.Generic;
using System.Text;
#if NETSTANDARD || DESKTOPCLR
using System.Runtime.CompilerServices;
#endif

namespace Utf8Json.Internal
{
    public static class BinaryUtil
    {
        const int ArrayMaxSize = 0x7FFFFFC7; // https://msdn.microsoft.com/en-us/library/system.array

#if NETSTANDARD || DESKTOPCLR
        [MethodImpl(InlineMethod.Value)]
#endif
        public static void EnsureCapacity(ref byte[] bytes, int offset, int appendLength)
        {
            var newLength = offset + appendLength;

            // If null(most case fisrt time) fill byte.
            if (bytes == null)
            {
                bytes = new byte[newLength];
                return;
            }

            // like MemoryStream.EnsureCapacity
            var current = bytes.Length;
            if (newLength > current)
            {
                int num = newLength;
                if (num < 256)
                {
                    num = 256;
                    FastResize(ref bytes, num);
                    return;
                }

                if (current == ArrayMaxSize)
                {
                    ThrowHelper.ThrowInvalidOperationException_Reached_MaximumSize();
                }

                var newSize = unchecked((current * 2));
                if (newSize < 0) // overflow
                {
                    num = ArrayMaxSize;
                }
                else
                {
                    if (num < newSize)
                    {
                        num = newSize;
                    }
                }

                FastResize(ref bytes, num);
            }
        }

        // Buffer.BlockCopy version of Array.Resize
#if NETSTANDARD || DESKTOPCLR
        [MethodImpl(InlineMethod.Value)]
#endif
        public static void FastResize(ref byte[] array, int newSize)
        {
            if (newSize < 0) ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.newSize);

            byte[] array2 = array;
            if (array2 == null)
            {
                array = new byte[newSize];
                return;
            }

            if (array2.Length != newSize)
            {
                byte[] array3 = new byte[newSize];
                var len = (array2.Length > newSize) ? newSize : array2.Length;
                Buffer.BlockCopy(array2, 0, array3, 0, len);
                array = array3;
            }
        }

#if NETSTANDARD || DESKTOPCLR
        [MethodImpl(InlineMethod.Value)]
#endif
        public static
#if NETSTANDARD || NET_4_5_GREATER
            unsafe
#endif
            byte[] FastCloneWithResize(byte[] src, int newSize)
        {
            if (newSize < 0) ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.newSize);
            if (src == null) return new byte[newSize];
            if (src.Length < newSize) ThrowHelper.ThrowException_Length();

            byte[] dst = new byte[newSize];

#if NETSTANDARD || NET_4_5_GREATER
            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
              Buffer.MemoryCopy(pSrc, pDst, dst.Length, newSize);
            }
#else
            Buffer.BlockCopy(src, 0, dst, 0, newSize);
#endif

            return dst;
        }
    }
}

using System;
using System.Runtime.InteropServices;

namespace Shared
{
    [StructLayout(LayoutKind.Sequential)]
    public struct MutableArraySegment<T>
    {
        public T[] Array;
        public int Offset;
        public int Count;

        public MutableArraySegment(T[] array, int offset, int count)
        {
            Array = array;
            Offset = offset;
            Count = count;
        }
    }
    [StructLayout(LayoutKind.Explicit)]
    public struct MutableToImmutableArraySegmentConverter
    {
        [FieldOffset(0)]
        public MutableArraySegment<byte> Mutable;
        [FieldOffset(0)]
        public ArraySegment<byte> Immutable;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct MutableToImmutableArraySegmentArrayConverter
    {
        [FieldOffset(0)]
        public MutableArraySegment<byte>[] MutableArray;
        [FieldOffset(0)]
        public ArraySegment<byte>[] ImmutableArray;
    }
}
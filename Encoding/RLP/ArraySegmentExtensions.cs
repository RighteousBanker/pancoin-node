using System;
using System.Collections.Generic;
using System.Text;

namespace Encoders
{
    public static class ArraySegmentExtensions  //from RLP librarry by Ciaran Jones
    {
        public static ArraySegment<T> Slice<T>(this ArraySegment<T> source, int start, int? end = null)
        {
            start += source.Offset;
            var count = end.HasValue ? end.Value : Math.Abs(start - source.Array.Length);

            return new ArraySegment<T>(source.Array, start, count);
        }

        public static T[] ToArray<T>(this ArraySegment<T> source)
        {
            T[] targetArray = new T[source.Count];
            Array.Copy(source.Array, source.Offset, targetArray, 0, source.Count);

            return targetArray;
        }
    }
}

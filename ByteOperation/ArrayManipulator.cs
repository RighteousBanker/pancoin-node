using System;
using System.Collections.Generic;
using System.Text;

namespace ByteOperation
{
    public static class ArrayManipulator
    {
        public static T[] SubArray<T>(T[] input, int offset, int count)
        {
            var ret = new T[count];

            for (int i = 0; i < count; i++)
            {
                ret[i] = input[i + offset];
            }

            return ret;
        }

        /// <summary>
        /// Big endian A > B
        /// </summary>
        public static bool Compare(byte[] a, byte[] b)
        {
            var ret = true;

            var length = a.Length > b.Length ? b.Length : a.Length;

            for (int i = 0; i < length; i++)
            {
                if (a[i] != b[i])
                {
                    ret = false;
                    break;
                }
            }

            return ret;
        }

        /// <summary>
        /// Big endian a > b. First byte is MSB
        /// </summary>
        public static bool IsGreater(byte[] a, byte[] b, int count, int startIndex = 0)
        {
            var ret = false;

            for (int i = startIndex; i < startIndex + count; i++)
            {
                if (a[i] == b[i])
                {
                    continue;
                }
                if (a[i] > b[i])
                {
                    ret = true;
                    break;
                }
                else
                {
                    break;
                }
            }

            return ret;
        }

        public static Dictionary<string, List<T>> DictionaryFromList<T>(List<T> list, Func<T, string> selector)
        {
            var ret = new Dictionary<string, List<T>>();

            foreach (var item in list)
            {
                var key = selector(item);

                if (ret.ContainsKey(key))
                {
                    ret[key].Add(item);
                }
                else
                {
                    ret.Add(key, new List<T>() { item });
                }
            }

            return ret;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ByteOperation
{
    public static class ByteManipulator
    {
        /// <summary>
        /// If count is higher than input then it increases size of input
        /// </summary>
        public static byte[] BigEndianTruncate(byte[] input, int count)
        {
            var ret = new byte[count];

            input = input.Reverse().ToArray();

            for (int i = 0; i < count ; i++)
            {
                if (i > input.Length - 1)
                {
                    break;
                }

                ret[i] = input[i];
            }

            return ret.Reverse().ToArray();
        }

        public static ulong? GetUInt64(byte[] input)
        {
            ulong? ret;

            if (input != null)
            {
                input = BigEndianTruncate(input, 8);

                if (BitConverter.IsLittleEndian)
                {
                    input = input.Reverse().ToArray();
                }

                ret = BitConverter.ToUInt64(input);
            }
            else
            {
                ret = null;
            }

            return ret;
        }

        public static uint GetUInt32(byte[] input)
        {
            uint ret = 0;

            if (input != null)
            {
                input = BigEndianTruncate(input, 4);

                if (BitConverter.IsLittleEndian)
                {
                    input = input.Reverse().ToArray();
                }

                ret = BitConverter.ToUInt32(input);
            }
            else
            {
                ret = 0;
            }

            return ret;
        }

        public static int? GetInt32(byte[] input)
        {
            int? ret;

            if (input != null && input.Length <= 4)
            {
                input = BigEndianTruncate(input, 8);

                if (BitConverter.IsLittleEndian)
                {
                    input = input.Reverse().ToArray();
                }

                ret = BitConverter.ToInt32(input);
            }
            else
            {
                ret = null;
            }

            return ret;
        }

        public static byte[] GetBytes(uint input)
        {
            var ret = BitConverter.GetBytes(input);

            if (BitConverter.IsLittleEndian)
            {
                ret = ret.Reverse().ToArray();
            }

            return ret;
        }

        public static byte[] TruncateMostSignificatZeroBytes(byte[] input)
        {
            byte[] ret;

            if (input != null)
            {
                int byteCounter = 0;

                for (byteCounter = 0; byteCounter < input.Length; byteCounter++)
                {
                    if (input[byteCounter] != 0)
                        break;
                }

                if (byteCounter != input.Length)
                {
                    ret = new byte[input.Length - byteCounter];

                    for (int i = 0; i < input.Length - byteCounter; i++)
                    {
                        ret[i] = input[i + byteCounter];
                    }
                }
                else
                {
                    ret = new byte[] { 0 };
                }
            }
            else
            {
                ret = null;
            }

            return ret;
        }

        public static byte[] AddPaddingByte(byte[] data)
        {
            byte[] ret;

            if (data[0] > 127)
            {
                ret = new byte[data.Length + 1];

                for (int i = 0; i < data.Length; i++)
                {
                    ret[i + 1] = data[i];
                }
            }
            else
            {
                ret = data;
            }

            return ret;
        }
    }
}

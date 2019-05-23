using System;
using System.Linq;
using System.Text;

namespace ByteOperation
{
    public class HexConverter
    {
        public static string ToString(byte[] bytes)  //from stack overflow
        {
            try
            {
                var ret = new StringBuilder(bytes.Length * 2);
                var hexAlphabet = "0123456789abcdef";

                foreach (byte B in bytes)
                {
                    ret.Append(hexAlphabet[(int)(B >> 4)]);
                    ret.Append(hexAlphabet[(int)(B & 0xF)]);
                }

                return ret.ToString();
            }
            catch
            {
                return null;
            }
        }

        public static string ToString(ulong data)
        {
            var bytes = BitConverter.GetBytes(data);

            if (BitConverter.IsLittleEndian)
            {
                bytes = bytes.Reverse().ToArray();
            }

            return ToString(bytes);
        }

        public static string ToString(int data)
        {
            var bytes = BitConverter.GetBytes(data);

            if (BitConverter.IsLittleEndian)
            {
                bytes = bytes.Reverse().ToArray();
            }

            return ToString(bytes);
        }

        public static string ToString(LargeInteger data)
        {
            return ToString(data.GetBytes());
        }

        public static string ToPrefixString(byte[] bytes)
        {
            return "0x" + ToString(bytes);
        }

        public static string ToPrefixString(ulong data)
        {
            return "0x" + ToString(data);
        }

        public static string ToPrefixString(int data)
        {
            return "0x" + ToString(data);
        }

        public static string ToPrefixString(LargeInteger data)
        {
            return "0x" + ToString(data);
        }

        public static byte[] ToBytes(string hex) //from stack overflow
        {
            try
            {
                if (hex != null || hex.Length == 0)
                {
                    if (hex.StartsWith("0x"))
                    {
                        hex = hex.Substring(2);
                    }

                    if (hex.Length % 2 != 0)
                    {
                        hex = "0" + hex;
                    }

                    byte[] bytes = new byte[hex.Length / 2];
                    int[] hexValue = new int[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F };

                    for (int x = 0, i = 0; i < hex.Length; i += 2, x += 1)
                    {
                        bytes[x] = (byte)(hexValue[char.ToUpper(hex[i + 0]) - '0'] << 4 |
                                          hexValue[char.ToUpper(hex[i + 1]) - '0']);
                    }

                    return bytes;
                }
                else
                {
                    return null;
                }
            }
            catch
            {
                return null;
            }
        }

        public static string ToRpcHex(byte[] bytes)
        {
            var hex = ToString(bytes);

            var hexBuilder = new StringBuilder();

            int i;
            for (i = 0; i < hex.Length; i++)
            {
                if (hex[i] != '0')
                {
                    break;
                }
            }

            return "0x" + hex.Substring(i);
        }

        public static string ToRpcHex(ulong data)
        {
            var bytes = BitConverter.GetBytes(data);

            if (BitConverter.IsLittleEndian)
            {
                bytes = bytes.Reverse().ToArray();
            }

            return ToRpcHex(bytes);
        }

        public static string ToRpcHex(LargeInteger data)
        {
            return ToRpcHex(data.GetBytes());
        }

        public static ulong ToULong(string hex)
        {
            var bytes = ToBytes(hex);

            bytes = ByteManipulator.BigEndianTruncate(bytes, 8);

            if (BitConverter.IsLittleEndian)
            {
                bytes = bytes.Reverse().ToArray();
            }

            return BitConverter.ToUInt64(bytes);
        }

        public static LargeInteger ToLargeInteger(string hex)
        {
            var bytes = ToBytes(hex);

            return new LargeInteger(bytes);
        }
    }
}

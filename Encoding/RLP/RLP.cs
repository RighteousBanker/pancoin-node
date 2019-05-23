using ByteOperation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Encoders
{
    public static class RLP //modified version of RLP library by Ciaran Jones
    {
        private const int SizeThreshold = 55;
        private const int ShortItemOffset = 128;
        private const int LargeItemOffset = 183;
        private const int ShortCollectionOffset = 192;
        private const int LargeCollectionOffset = 247;
        private const int MaxItemLength = 255;

        public static byte[] Encode(byte[] bytes)
        {
            if (bytes == null)
            {
                return null;
            }
            else
            {
                bytes = ByteManipulator.TruncateMostSignificatZeroBytes(bytes);
            }

            if (bytes.Length == 1 && bytes[0] < 128)
            {
                return new byte[] { bytes[0] };
            }

            if (bytes.Length == 0)
            {
                return new byte[0];
            }

            if (bytes[0] == 0)
            {
                return new byte[0];
            }

            if (bytes.Length <= SizeThreshold)
            {
                var newBytes = new byte[bytes.Length + 1];
                newBytes[0] = Convert.ToByte(ShortItemOffset + bytes.Length);
                Array.Copy(bytes, 0, newBytes, 1, bytes.Length);

                return newBytes;
            }
            else
            {
                var lengthBytesCount = GetLength(bytes.Length);
                var lengthBytes = ByteManipulator.GetBytes((uint)bytes.Length);

                var newBytes = new byte[bytes.Length + lengthBytesCount + 1];
                newBytes[0] = Convert.ToByte(LargeItemOffset + lengthBytesCount);

                for (int i = lengthBytes.Length - 1; i >= lengthBytes.Length - lengthBytesCount; i--)
                {
                    newBytes[i - 3 + lengthBytesCount] = lengthBytes[i];
                }

                Array.Copy(bytes, 0, newBytes, 1 + lengthBytesCount, bytes.Length);

                return newBytes;
            }
        }

        public static byte[] Encode(IEnumerable<byte[]> input)
        {
            var items = new List<byte[]>();
            var totalLength = 0;

            foreach (var bytes in input.Select(Encode))
            {
                if (bytes != null && bytes.Length != 0 && bytes[0] != 0)
                {
                    items.Add(bytes);
                    totalLength += bytes.Length;
                }
                else
                {
                    items.Add(new byte[] { 0x80 }); //empty byte[]
                    totalLength += 1;
                }
            }

            if (totalLength == 0x80)
            {
                items.Insert(0, new byte[] { 0x80 });
            }
            else if (totalLength <= SizeThreshold)
            {
                items.Insert(0, new[] { Convert.ToByte(ShortCollectionOffset + totalLength) });
            }
            else
            {
                var lengthBytes = ByteManipulator.GetBytes((uint)totalLength);

                var lengthBytesCount = GetLength(totalLength);

                items.Insert(0, new[] { Convert.ToByte(LargeCollectionOffset + lengthBytesCount) });

                for (int i = lengthBytes.Length - 1; i >= lengthBytes.Length - lengthBytesCount; i--)
                {
                    items.Insert(1, new[] { lengthBytes[i] });
                }
            }

            return items.SelectMany(x => x).ToArray();
        }

        public static IList<byte[]> Decode(byte[] input)
        {
            var message = new RLPMessage(input);

            while (message.Remainder.Offset < input.Length)
            {
                Decode(message);
            }

            return message.Decoded;
        }

        private static void Decode(RLPMessage msg)
        {
            var firstByte = msg.Remainder.Array[msg.Remainder.Offset];

            if (firstByte == 0x80)
            {
                msg.Decoded.Add(null);
                msg.Remainder = msg.Remainder.Slice(1);
                return;
            }

            // single byte
            if (firstByte <= 0x7f)
            {
                msg.Decoded.Add(new byte[] { firstByte });
                msg.Remainder = msg.Remainder.Slice(1);
                return;
            }

            // string <55 bytes
            if (firstByte <= 0xb7)
            {
                var itemLength = Math.Abs(128 - firstByte);
                var data = firstByte == 0x80 ? new ArraySegment<byte>(new byte[0]) : msg.Remainder.Slice(1, itemLength);
                msg.Decoded.Add(ArrayManipulator.SubArray(data.Array, data.Offset, data.Count));
                msg.Remainder = msg.Remainder.Slice(data.Count + 1);
                return;
            }

            // string >55 bytes
            if (firstByte <= 0xbf)
            {
                var lengthBytesCount = Math.Abs(LargeItemOffset - firstByte);
                var lengthBytes = new byte[4];

                var startIndex = Math.Abs(lengthBytesCount - 5) - 1;

                for (int i = 0; i < lengthBytesCount; i++)
                {
                    lengthBytes[startIndex + i] = msg.Remainder.Array[msg.Remainder.Offset + i + 1];
                }

                var itemLength = ByteManipulator.GetUInt32(lengthBytes);

                var data = msg.Remainder.Slice(lengthBytesCount + 1, (int)itemLength);

                msg.Decoded.Add(ArrayManipulator.SubArray(msg.Remainder.Array, data.Offset, data.Count));
                msg.Remainder = msg.Remainder.Slice(data.Count + lengthBytesCount + 1);
                return;
            }

            // collection <55 bytes
            if (firstByte <= 0xf7)
            {
                var itemLength = Math.Abs(192 - firstByte);
                var data = msg.Remainder.Slice(1, itemLength).ToArray();

                while (msg.Remainder.Offset < msg.Remainder.Array.Length)
                {
                    var decoded = Decode(data);
                    msg.Decoded.AddRange(decoded);
                    msg.Remainder = msg.Remainder.Slice(msg.Remainder.Count);
                }

                return;
            }

            // collection >55 bytes
            if (firstByte <= 0xff)
            {
                var lengthBytesCount = Math.Abs(247 - firstByte);
                var lengthBytes = new byte[4];

                var startIndex = Math.Abs(lengthBytesCount - 5) - 1;

                for (int i = 0; i < lengthBytesCount; i++)
                {
                    lengthBytes[startIndex + i] = msg.Remainder.Array[msg.Remainder.Offset + i + 1];
                }

                var itemLength = ByteManipulator.GetUInt32(lengthBytes);

                var data = msg.Remainder.Slice(lengthBytesCount + 1, (int)itemLength).ToArray();

                while (msg.Remainder.Offset < msg.Remainder.Array.Length)
                {
                    var decoded = Decode(data);
                    msg.Decoded.AddRange(decoded);
                    msg.Remainder = msg.Remainder.Slice(msg.Remainder.Count);
                }

                return;
            }
        }

        static int GetLength(int totalLength)
        {
            int lengthBytesCount;

            if (totalLength < 256)
            {
                lengthBytesCount = 1;
            }
            else if (totalLength < 65536)
            {
                lengthBytesCount = 2;
            }
            else if (totalLength < 16777216)
            {
                lengthBytesCount = 3;
            }
            else
            {
                lengthBytesCount = 4;
            }

            return lengthBytesCount;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using ByteOperation;

namespace Encoders
{
    public class SerialEncoder : ISerialCoderLookup
    {
        public int LengthBytes { get; private set; }
        public long FileLength { get; private set; }
        public int Count { get; private set; }

        private long offset = 0;
        private string path;

        private FileStream fileStream;

        //[length][data][length][data]...
        //little endian length
        
        public SerialEncoder(string path, int lengthBytes)
        {
            if (lengthBytes > 4 || lengthBytes < 1)
            {
                throw new Exception("Length bytes must be 4 or less");
            }

            this.path = path;
            CreateStream();
            FileLength = fileStream.Length;
            LengthBytes = lengthBytes;
            RecalculateCount();
        }

        public byte[] ReadNext()
        {
            byte[] ret = null;

            if (offset < FileLength)
            {
                var dataLengthBuffer = new byte[LengthBytes];

                fileStream.Position = offset;
                fileStream.Read(dataLengthBuffer);
                fileStream.Flush();

                offset += LengthBytes;
                
                var dataLength = ByteManipulator.GetUInt32(dataLengthBuffer);

                ret = new byte[dataLength];

                fileStream.Position = offset;
                fileStream.Read(ret);
                fileStream.Flush();

                offset += ret.Length;
            }

            return ret;
        }

        public byte[] ReadByOffset(long offset)
        {
            this.offset = offset;

            return ReadNext();
        }

        public byte[] ReadByIndex(int index)
        {
            offset = GetIndexOffset(index);

            return ReadNext();
        }

        public List<byte[]> ReadDataByIndex(int startIndex = 0)
        {
            offset = GetIndexOffset(startIndex);

            return ReadData();
        }

        public List<byte[]> ReadDataByOffset(long offset = 0)
        {
            this.offset = offset;

            return ReadData();
        }

        private List<byte[]> ReadData()
        {
            var ret = new List<byte[]>();
            byte[] data;
            bool finished = false;

            while (!finished)
            {
                data = ReadNext();
                finished = data == null;

                if (!finished)
                {
                    ret.Add(data);
                }
            }

            return ret;
        }

        public bool VerifyDataIntegrity()
        {
            return false;
        }

        public void Append(byte[] data)
        {
            var bytes = new byte[LengthBytes + data.Length];

            var lengthData = ByteManipulator.GetBytes((uint)data.Length);

            for (int i = 0; i < LengthBytes; i++)
            {
                bytes[i] = lengthData[i + 4 - LengthBytes];
            }

            for (int i = 0; i < data.Length; i++)
            {
                bytes[i + LengthBytes] = data[i];
            }

            fileStream.Position = FileLength;
            fileStream.Write(bytes);
            fileStream.Flush();

            FileLength = fileStream.Length;
            Count++;
        }

        public void Append(byte[][] data)
        {
            foreach (var byteArray in data)
            {
                Append(byteArray);
            }
        }

        public void Replace(byte[][] data)
        {
            fileStream.SetLength(0);
            Count = 0;
            FileLength = 0;
            offset = 0;

            Append(data);
        }

        public void RemoveByIndex(int index)
        {
            RemoveByOffset(GetIndexOffset(index));
        }

        public void RemoveByOffset(long offset)
        {
            var buffer = ReadDataByOffset(NextOffset(offset)).ToArray();

            fileStream.SetLength(offset);

            FileLength = fileStream.Length;
            Count--;

            Append(buffer);
        }

        public long NextOffset(long start)
        {
            var buffer = new byte[LengthBytes];

            fileStream.Position = start;
            fileStream.Read(buffer);
            fileStream.Flush();

            var length = ByteManipulator.GetUInt32(buffer);

            return start + length + LengthBytes;
        }

        public long GetIndexOffset(int index)
        {
            var dataStart = 0L;

            for (int i = 0; i < index; i++)
            {
                dataStart = NextOffset(dataStart);
            }

            return dataStart;
        }

        public void ChangeValueByOffset(long offset, byte[] value)
        {
            this.offset = offset;

            var dataLengthBuffer = new byte[LengthBytes];

            fileStream.Position = offset;
            fileStream.Read(dataLengthBuffer);
            fileStream.Flush();
            
            if (ByteManipulator.GetUInt32(dataLengthBuffer) != value.Length)
            {
                throw new Exception("Value length does not match data length");
            }

            fileStream.Position = offset + LengthBytes;
            fileStream.Write(value);
            fileStream.Flush();
        }

        public void ChangeValueByIndex(int index, byte[] value)
        {
            ChangeValueByOffset(GetIndexOffset(index), value);
        }
        
        public void Delete()
        {
            Dispose();
            File.Delete(path);
        }

        public void Dispose()
        {
            fileStream.Dispose();
        }

        private void CreateStream()
        {
            if (fileStream != null)
            {
                fileStream.Dispose();
            }

            fileStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }

        private void RecalculateCount()
        {
            Count = 0;
            offset = 0;

            while (offset < FileLength)
            {
                offset = NextOffset(offset);
                Count++;
            }
        }
    }
}

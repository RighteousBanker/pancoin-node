using ByteOperation;
using System;
using System.Collections.Generic;
using System.IO;

namespace Encoders
{
    public class LinearEncoder
    {
        public long FileLength { get; private set; }
        public int DataLength { get; private set; }
        public int Count { get; private set; }

        long offset = 0;
        string path;
        Random random;
         
        private FileStream fileStream;

        //[data][data][data]... (constant data length)

        public LinearEncoder(string path, int dataByteLength)
        {
            DataLength = dataByteLength;

            this.path = path;
            random = new Random();
            
            CreateStream();
        }

        public void Remove(int index)
        {
            if (index >= Count)
            {
                throw new IndexOutOfRangeException();
            }

            var buffers = new List<byte[]>();

            while (Count != index + 1)
            {
                buffers.Add(Pop());
            }

            Pop();

            for (int i = buffers.Count - 1; i >= 0; i--)
            {
                Push(buffers[i]);
            }
        }

        public byte[] Pop()
        {
            byte[] ret;

            if (Count != 0)
            {
                offset = FileLength - DataLength;

                ret = ReadNext();

                fileStream.SetLength(FileLength - DataLength);

                ReevaluateProperties();
            }
            else
            {
                ret = null;
            }

            return ret;
        }

        public byte[] ReadNext()
        {
            byte[] ret = null;

            if (offset < FileLength)
            {
                ret = new byte[DataLength];

                fileStream.Position = offset;

                fileStream.Read(ret);
                
                offset += ret.Length;
            }

            return ret;
        }

        public byte[] Read(int index)
        {
            offset = index * DataLength;

            return ReadNext();
        }

        public List<byte[]> BulkPop(int count)
        {
            var ret = new List<byte[]>();

            offset = FileLength - (count * DataLength);

            var buffer = new byte[count * DataLength];

            fileStream.Position = offset;

            fileStream.Read(buffer);

            var entry = new byte[DataLength];

            for (int i = 0; i < buffer.Length; i++)
            {
                if (i % DataLength == 0 && i != 0)
                {
                    ret.Add(entry);
                    entry = new byte[DataLength];
                }

                entry[i % DataLength] = buffer[i];
            }

            ret.Add(entry);

            fileStream.SetLength(FileLength - (DataLength * count));

            ReevaluateProperties();

            return ret;
        }

        public void BulkPush(List<byte[]> data)
        {
            var buffer = new byte[data.Count * DataLength];

            int counter = 0;

            foreach (var entry in data)
            {
                var extendedEntry = ByteManipulator.BigEndianTruncate(entry, DataLength);

                for (int i = 0; i < DataLength; i++)
                {
                    buffer[counter++] = extendedEntry[i];
                }
            }

            fileStream.Write(buffer);
            fileStream.Flush();

            ReevaluateProperties();
        }

        public List<byte[]> ReadData(int startIndex = 0)
        {
            var ret = new List<byte[]>();
            byte[] data;
            offset = startIndex * DataLength;

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

        public void Push(byte[] data)
        {
            if (data.Length > DataLength)
            {
                throw new Exception("Data are longer than datalength");
            }

            var buffer = new byte[DataLength];

            for (int i = 0; i < data.Length; i++)
            {
                buffer[i] = data[i];
            }

            fileStream.Write(buffer);
            fileStream.Flush();

            ReevaluateProperties();
        }

        public void Append(byte[][] data)
        {
            foreach (var byteArray in data)
            {
                Push(byteArray);
            }
        }

        public void Replace(int index, byte[] data)
        {
            var offset = index * DataLength;

            if (data.Length > DataLength)
            {
                throw new Exception("Data length exceeded datalength");
            }

            var buffer = new byte[DataLength];

            for (int i = 0; i < data.Length; i++)
            {
                buffer[i] = data[i];
            }

            fileStream.Position = offset;
            fileStream.Write(buffer);
            fileStream.Flush();
        }

        public void Rename(string newPath)
        {
            fileStream.Dispose();
            fileStream = null;

            File.Move(path, newPath);

            path = newPath;

            CreateStream();
        }

        public bool VerifyFileLength()
        {
            return FileLength % DataLength == 0;
        }

        public void Dispose()
        {
            fileStream.Dispose();
        }

        public void Delete()
        {
            Dispose();
            File.Delete(path);
        }

        private void ReevaluateProperties()
        {
            FileLength = fileStream.Length;
            Count = (int)(FileLength / DataLength);
        }

        private void CreateStream()
        {
            if (fileStream != null)
            {
                fileStream.Dispose();
            }

            fileStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

            ReevaluateProperties();
        }
    }
}

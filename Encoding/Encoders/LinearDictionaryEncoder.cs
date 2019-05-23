using ByteOperation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Encoders
{
    public class LinearDictionaryEncoder
    {
        public int KeyLength { get; private set; }
        public int DataLength { get; private set; }
        public LinearEncoder LinearCoder;

        int bulkOperationCount = 1000;

        public LinearDictionaryEncoder(string path, int keyLength, int dataLength)
        {
            KeyLength = keyLength;
            DataLength = dataLength;

            LinearCoder = new LinearEncoder(path, keyLength + dataLength);
        }

        public void Add(byte[] key, byte[] value)
        {
            value = ByteManipulator.BigEndianTruncate(value, DataLength);
            key = ByteManipulator.BigEndianTruncate(key, KeyLength);

            if (LinearCoder.Count == 0)
            {
                LinearCoder.Push(CreateEntry(key, value));
            }
            else
            {
                int minimum = 0;
                int maximum = LinearCoder.Count - 1;

                while (minimum <= maximum)
                {
                    int middle = (minimum + maximum) / 2;

                    var middleValue = LinearCoder.Read(middle);

                    if (ArrayManipulator.Compare(key, middleValue.Take(key.Length).ToArray()))
                    {
                        throw new Exception("Key already exists");
                    }
                    else if (ArrayManipulator.IsGreater(key, middleValue, KeyLength))
                    {
                        minimum = middle + 1;
                    }
                    else
                    {
                        maximum = middle - 1;
                    }
                }

                var popCount = LinearCoder.Count - minimum;
                
                if ((maximum >= 0) && ArrayManipulator.IsGreater(LinearCoder.Read(maximum), key, KeyLength))
                {
                    popCount++;
                }

                List<byte[]> popedItems;

                if (popCount != 0)
                {
                    popedItems = LinearCoder.BulkPop(popCount);
                }
                else
                {
                    popedItems = new List<byte[]>();
                }

                popedItems.Insert(0, CreateEntry(key, value));

                LinearCoder.BulkPush(popedItems);
            }
        }

        public void Replace(byte[] key, byte[] value)
        {
            var buffer = CreateEntry(key, value);

            LinearCoder.Replace(GetIndex(key), buffer);
        }

        public void Remove(byte[] key)
        {
            key = ByteManipulator.BigEndianTruncate(key, KeyLength);
            LinearCoder.Remove(GetIndex(key));
        }

        public byte[] Get(byte[] key)
        {
            key = ByteManipulator.BigEndianTruncate(key, KeyLength);

            byte[] ret = null;
            var index = GetIndex(key);
            
            if (index != -1)
            {
                ret = LinearCoder.Read(index);
                ret = ArrayManipulator.SubArray(ret, KeyLength, DataLength);
            }

            return ret;
        }

        public List<byte[]> GetRawData()
        {
            return LinearCoder.ReadData();
        }

        public void Delete()
        {
            LinearCoder.Delete();
        }

        public void Dispose()
        {
            LinearCoder.Dispose();
        }

        private void Push(byte[] key, byte[] value)
        {
            var buffer = CreateEntry(key, value);

            LinearCoder.Push(buffer);
        }

        private bool CountainsKey(byte[] key)
        {
            var result = GetIndex(key);

            return result != -1 ? true : false;
        }

        public bool ContainsKey(byte[] key)
        {
            key = ByteManipulator.BigEndianTruncate(key, KeyLength);

            if (GetIndex(key) < 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        private int GetIndex(byte[] key)
        {
            if (LinearCoder.Count == 1)
            {
                var value = LinearCoder.Read(0);

                if (!ArrayManipulator.Compare(key, value))
                {
                    return -1;
                }

                return 0;
            }
            else if (key != null && key.Length == KeyLength)
            {
                int minimum = 0;
                int maximum = LinearCoder.Count - 1;

                while (minimum <= maximum)
                {
                    int middle = (minimum + maximum) / 2;

                    var middleValue = LinearCoder.Read(middle);

                    if (ArrayManipulator.Compare(key, middleValue.Take(key.Length).ToArray()))
                    {
                        return middle;
                    }
                    else if (ArrayManipulator.IsGreater(key, middleValue, KeyLength))
                    {
                        minimum = middle + 1;
                    }
                    else
                    {
                        maximum = middle - 1;
                    }
                }
                return -1;
            }
            else
            {
                throw new Exception("Invalid key format");
            }
        }

        byte[] CreateEntry(byte[] key, byte[] value)
        {
            key = ByteManipulator.BigEndianTruncate(key, KeyLength);
            value = ByteManipulator.BigEndianTruncate(value, DataLength);

            var buffer = new byte[KeyLength + DataLength];

            for (int i = 0; i < key.Length; i++)
            {
                buffer[i] = key[i];
            }

            for (int i = 0; i < value.Length; i++)
            {
                buffer[i + key.Length] = value[i];
            }

            return buffer;
        }
    }
}

using ByteOperation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Encoders
{
    public class SerialDictionaryEncoder
    {
        public int KeyLength { get; private set; }
        public int DataLengthBytesCount { get; private set; }
        public int Count { get; private set; }
        public ISerialCoderLookup Lookup { get; private set; }

        public LinearDictionaryEncoder LinearDictionaryCoder;
        LinearDictionaryEncoder reverseLookupLinearDictionaryCoder;
        SerialEncoder serialCoder;

        public SerialDictionaryEncoder(string dataPath, string lookupPath, int keyLength, int dataLengthBytesCount)
        {
            KeyLength = keyLength;
            DataLengthBytesCount = dataLengthBytesCount;

            LinearDictionaryCoder = new LinearDictionaryEncoder(lookupPath, keyLength, 4);
            reverseLookupLinearDictionaryCoder = new LinearDictionaryEncoder(lookupPath + "_reverse", 4, keyLength);

            serialCoder = new SerialEncoder(dataPath, dataLengthBytesCount);
            Lookup = serialCoder;
            Count = serialCoder.Count;
        }

        public void Add(byte[] key, byte[] value)
        {
            LinearDictionaryCoder.Add(key, ByteManipulator.GetBytes((uint)serialCoder.FileLength));
            reverseLookupLinearDictionaryCoder.Add(ByteManipulator.GetBytes((uint)serialCoder.FileLength), key);
            serialCoder.Append(value);
            Count++;
        }
        
        public void RemoveTopItem()
        {
            var topItemOffset = serialCoder.GetIndexOffset(serialCoder.Count - 1);
            serialCoder.RemoveByOffset(topItemOffset);

            var reverseKey = ByteManipulator.GetBytes((uint)topItemOffset);
            var key = reverseLookupLinearDictionaryCoder.Get(reverseKey);

            LinearDictionaryCoder.Remove(key);
            reverseLookupLinearDictionaryCoder.Remove(reverseKey);
        }

        public byte[] Get(byte[] key)
        {
            byte[] ret = null;
            var offset = GetOffset(key);

            if (offset.HasValue)
            {
                ret = serialCoder.ReadByOffset(offset.Value);
            }

            return ret;
        }

        public List<byte[]> GetRawData()
        {
            return serialCoder.ReadDataByIndex();
        }

        public bool ContainsKey(byte[] key)
        {
            return LinearDictionaryCoder.ContainsKey(key);
        }

        public void Dispose()
        {
            LinearDictionaryCoder.Dispose();
            serialCoder.Dispose();
            reverseLookupLinearDictionaryCoder.Dispose();
        }

        public void Delete()
        {
            serialCoder.Delete();
            LinearDictionaryCoder.Delete();
            reverseLookupLinearDictionaryCoder.Delete();
        }

        private uint? GetOffset(byte[] key)
        {
            uint? ret = null;
            var offset = LinearDictionaryCoder.Get(key);
            
            if (offset != null)
            {
                ret = ByteManipulator.GetUInt32(offset);
            }

            return ret;
        }
    }
}

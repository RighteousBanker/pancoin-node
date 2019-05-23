using ByteOperation;
using Encoders;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Tests
{
    public static class CoderReader
    {
        public static List<LargeInteger> ReadLinearCoder(LinearEncoder coder)
        {
            var data = coder.ReadData(0);

            var ret = new List<LargeInteger>();

            foreach (var byteArray in data)
            {
                ret.Add(new LargeInteger(byteArray));
            }

            return ret;
        }

        public static Dictionary<byte[], LargeInteger> ReadLinearDictionaryCoder(LinearDictionaryEncoder coder)
        {
            var data = coder.LinearCoder.ReadData(0);

            var ret = new Dictionary<byte[], LargeInteger>();

            foreach (var byteArray in data)
            {
                ret.Add(byteArray.Take(coder.KeyLength).ToArray(), new LargeInteger(byteArray.Skip(coder.KeyLength).Take(coder.DataLength).ToArray()));
            }

            return ret;
        }
    }
}

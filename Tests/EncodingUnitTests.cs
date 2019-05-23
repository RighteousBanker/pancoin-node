using ByteOperation;
using Encoders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Tests
{
    [TestClass]
    public class EncodingUnitTests
    {
        [TestMethod]
        public void LinearOffsetCoderUnitTest()
        {
            string path = "linearCoderTest";

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            var linearCoder = new LinearEncoder(path, 64);

            var expected = new byte[15][];

            for (int i = 0; i < expected.Length; i++)
            {
                expected[i] = new byte[64];

                for (int j = 0; j < 64; j++)
                {
                    expected[i][j] = (byte)(33 + j + i); //test data
                }
            }

            linearCoder.Append(expected);

            var actual = linearCoder.ReadData();

            for (int i = 0; i < expected.Length; i++)
            {
                Assert.IsTrue(ArrayManipulator.Compare(expected[i], actual[i]));
            }

            Assert.AreEqual(expected.Length, linearCoder.Count);

            linearCoder.Remove(1);

            Assert.AreEqual((byte)35, linearCoder.ReadData()[1][0]);
        }

        [TestMethod]
        public void SerialCoderUnitTest()
        {
            string path = "serialCoderTest";

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            var serialCoder = new SerialEncoder(path, 2);

            var expected = new byte[][]
            {
                new byte[42],
                new byte[253],
                new byte[1250],
                new byte[8710],
                new byte[25]
            };

            for (int i = 0; i < expected.Length; i++)
            {
                for (int j = 0; j < expected[i].Length; j++)
                {
                    expected[i][j] = (byte)(i + 40); //test data
                }
            }

            serialCoder.Replace(expected);

            var actual = serialCoder.ReadDataByIndex();

            for (int i = 0; i < expected.Length; i++)
            {
                Assert.IsTrue(ArrayManipulator.Compare(expected[i], actual[i]));
            }

            serialCoder.RemoveByIndex(2);

            serialCoder.RemoveByIndex(3);

            actual = serialCoder.ReadDataByIndex();

            Assert.IsTrue(actual[2].Length == 8710);

            var value = serialCoder.ReadByIndex(1);

            for (int i = 0; i < value.Length; i++)
            {
                value[i] += 10;
            }

            serialCoder.ChangeValueByIndex(1, value);

            var actualValue = serialCoder.ReadByIndex(1);

            Assert.IsTrue(ArrayManipulator.Compare(actualValue, value));
        }

        [TestMethod]
        public void LinearDictionaryCoder()
        {
            var testPath = "linearDictionaryUnitTest";

            if (File.Exists(testPath))
            {
                File.Delete(testPath);
            }

            var linearDictionaryCoder = new LinearDictionaryEncoder(testPath, 32, 32);

            var expectedDictionary = new Dictionary<byte[], byte[]>();

            LargeInteger value = 0;

            for (int i = 0; i < 1500; i++)
            {
                var valueBytes = ByteManipulator.BigEndianTruncate(value.GetBytes(), 32);
                
                var key = CryptographyHelper.Sha3256(valueBytes);

                linearDictionaryCoder.Add(key, valueBytes);
                expectedDictionary.Add(key, valueBytes);

                value = value + 1;
            }

            foreach (var kvp in expectedDictionary)
            {
                var entryValue = linearDictionaryCoder.Get(kvp.Key);
                Assert.IsTrue(ArrayManipulator.Compare(kvp.Value, entryValue));
            }
        }

        [TestMethod]
        public void SerialDictionaryCoder()
        {
            var testDataPath = "serialDictionaryUnitTestData";
            var testLookupPath = "serialDictionaryUnitTestLookup";
            var testReverseLookupPath = "serialDictionaryUnitTestLookup_reverse";

            if (File.Exists(testDataPath))
            {
                File.Delete(testDataPath);
            }
            if (File.Exists(testLookupPath))
            {
                File.Delete(testLookupPath);
            }
            if (File.Exists(testReverseLookupPath))
            {
                File.Delete(testReverseLookupPath);
            }

            var serialDictionaryCoder = new SerialDictionaryEncoder(testDataPath, testLookupPath, 32, 2);

            var expectedDictionary = new Dictionary<byte[], byte[]>();

            for (int i = 0; i < 50; i++)
            {
                var value = new byte[i + 1];

                for (int j = 0; j < value.Length; j++)
                {
                    value[j] = (byte)(40 + i);
                }
                 
                var key = CryptographyHelper.Sha3256(value);

                serialDictionaryCoder.Add(key, value);
                expectedDictionary.Add(key, value);
            }
        }
    }
}

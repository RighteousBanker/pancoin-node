using ByteOperation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class ByteManipulatorUnitTests
    {
        [TestMethod]
        public void TruncateZeroBytesUnitTest()
        {
            var input = new byte[] { 0, 0, 0, 123, 45, 0, 11 };
            var expected = new byte[] { 123, 45, 0, 11 };

            var actual = ByteManipulator.TruncateMostSignificatZeroBytes(input);

            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i], actual[i]);
            }
        }

        [TestMethod]
        public void TruncateUnitTest()
        {
            var expected = new byte[] { 0, 0, 0, 123, 45, 0, 11 };
            var input = new byte[] { 123, 45, 0, 11 };

            var actual = ByteManipulator.BigEndianTruncate(input, 7);

            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i], actual[i]);
            }
        }

        [TestMethod]
        public void ExtendUnitTest()
        {
            var input = new byte[] { 21, 5, 128, 123, 45, 0, 11 };
            var expected = new byte[] { 128, 123, 45, 0, 11 };

            var actual = ByteManipulator.BigEndianTruncate(input, 5);

            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i], actual[i]);
            }
        }

        [TestMethod]
        public void AddPaddingByteTest()
        {
            var input1 = new byte[] { 129, 56, 0, 36 };
            var expected1 = new byte[] { 0, 129, 56, 0, 36 };

            var actual1 = ByteManipulator.AddPaddingByte(input1);

            Assert.AreEqual(expected1.Length, actual1.Length);
            Assert.IsTrue(actual1[0] == 0);

            var input2 = new byte[] { 127, 56, 0, 36 };
            var expected2 = new byte[] { 127, 56, 0, 36 };

            var actual2 = ByteManipulator.AddPaddingByte(input2);

            Assert.AreEqual(expected1.Length, actual1.Length);
            Assert.IsTrue(actual2[0] == 127);
        }

        [TestMethod]
        public void LittleEndianByteCompareUnitTest()
        {
            var inputA = new byte[] { 127, 56, 212, 36 };
            var inputB = new byte[] { 127, 56, 14, 36};

            var actual = ArrayManipulator.IsGreater(inputA, inputB, inputA.Length);

            Assert.IsTrue(actual);
        }
    }
}

using ByteOperation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Panode.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace Tests
{
    [TestClass]
    public class RlpUnitTests
    {
        [TestMethod]
        public void BlockSerializationUnitTest()
        {
            var wallet = new Wallet(HexConverter.ToBytes("0x03b28e661bd1aeae3919fd83706f969c5cde6994b161089ce96306f34cb87322"));

            var sender = new Wallet();
            var recipient = new Wallet();

            var expected = new Block()
            {
                Difficulty = new LargeInteger("10000000000000"),
                Height = 256,
                MinerAddress = wallet.PublicKey,
                Nonce = new byte[] { 1, 0 },
                Timestamp = 65464,
                PreviousBlockHash = new byte[32],
                Transactions = new List<Transaction>()
                {
                    ChainManagerUnitTests.CreateTransaction(sender, recipient, 1337, 0),
                    ChainManagerUnitTests.CreateTransaction(recipient, sender, 9000, 0)
                }
            };

            var bytes = expected.Serialize();

            var actual = new Block(bytes);

            Assert.IsTrue(ArrayManipulator.Compare(bytes, actual.Serialize()));
        }
    }
}

using ByteOperation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Panode.Core;

namespace Tests
{
    [TestClass]
    public class TransactionsUnitTests
    {
        [TestMethod]
        public void Signing()
        {
            for (int i = 0; i < 100; i++)
            {
                var wallet = new Wallet();

                Transaction tx = new Transaction()
                {
                    Ammount = 2000015161,
                    Fee = 6000,
                    Source = wallet.PublicKey,
                    Destination = HexConverter.ToBytes("bae"),
                    Nonce = 0,
                    Network = new byte[] { 1 }
                };

                tx.Sign(wallet.PrivateKey);

                Assert.IsTrue(tx.Verify());
            }
        }
    }
}

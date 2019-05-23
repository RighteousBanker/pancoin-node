using ByteOperation;
using System;

namespace Panode.Core
{
    public class Wallet
    {
        public byte[] PublicKey { get; set; }
        public byte[] PrivateKey { get; set; }

        public Wallet()
        {
            byte[] privateKey = CryptographyHelper.GeneratePrivateKeySecp256k1();

            GenerateWallet(privateKey);
        }

        public Wallet(byte[] privateKey)
        {
            if (privateKey.Length == 32)
            {
                GenerateWallet(privateKey);
            }
            else
            {
                throw new Exception("Private key length is not 32 bytes");
            }
        }

        private void GenerateWallet(byte[] privateKey)
        {
            PrivateKey = privateKey;

            PublicKey = CryptographyHelper.GeneratePublicKeySecp256k1(privateKey);
        }
    }
}

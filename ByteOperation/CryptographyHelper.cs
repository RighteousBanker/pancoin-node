using System.Security.Cryptography;
using DZen.Security.Cryptography;
using UChainDB.BingChain.Engine.Cryptography;

namespace ByteOperation
{
    public static class CryptographyHelper
    {
        static readonly SHA3256Managed sha3 = new SHA3256Managed();
        static readonly RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
        static readonly LargeInteger secp256k1PrivateKeyMaxValue = new LargeInteger(HexConverter.ToBytes("FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFEBAAEDCE6AF48A03BBFD25E8CD0364141"));
        static readonly Secp256k1 secp256k1 = new Secp256k1();

        static object sha3Lock = new object();
        static object ecdsaLock = new object();

        static CryptographyHelper()
        {
            sha3.UseKeccakPadding = false;
        }

        public static byte[] GenerateSecureRandomByteArray(int length)
        {
            var randomArray = new byte[length];
            rng.GetBytes(randomArray);

            return randomArray;
        }

        public static byte[] Sha3256(byte[] bytes)
        {
            lock (sha3Lock)
            {
                return sha3.ComputeHash(bytes);
            }
        }

        public static byte[] Sha3256(string hex)
        {
            lock (sha3Lock)
            {
                return sha3.ComputeHash(HexConverter.ToBytes(hex));
            }
        }

        public static byte[] GeneratePrivateKeySecp256k1()
        {
            lock (ecdsaLock)
            {
                byte[] privateKey;

                do
                {
                    privateKey = GenerateSecureRandomByteArray(32);
                }
                while (new LargeInteger(privateKey) > secp256k1PrivateKeyMaxValue);

                return privateKey;
            }
        }

        public static byte[] GeneratePublicKeySecp256k1(byte[] privateKey)
        {
            lock (ecdsaLock)
            {
                return secp256k1.GetPublicKey(privateKey);
            }
        }

        public static byte[] GenerateSignatureSecp256k1(byte[] privateKey, byte[] data)
        {
            lock (ecdsaLock)
            {
                return secp256k1.Sign(privateKey, data);
            }
        }

        public static bool Secp256k1Verify(byte[] publicKey, byte[] signature, byte[] data)
        {
            lock (ecdsaLock)
            {
                return secp256k1.Verify(publicKey, signature, data);
            }
        }
    }
}

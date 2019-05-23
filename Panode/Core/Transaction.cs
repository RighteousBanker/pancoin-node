using ByteOperation;
using Encoders;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Panode.Core
{
    public class Transaction
    {
        public uint Nonce { get; set; } //first nonce is 0
        public LargeInteger Ammount { get; set; } //integer ammount in smallest units
        public LargeInteger Fee { get; set; }
        public byte[] Source { get; set; } //ecdsa public key of sender
        public byte[] Destination { get; set; } //ecdsa public key of recipient
        public byte[] Signature { get; set; } //XY not DER
        public byte[] Network { get; set; }

        public Transaction() { }

        public Transaction(string hexRLP)
        {
            Deserialize(HexConverter.ToBytes(hexRLP));
        }

        public Transaction(byte[] hexRLP)
        {
            Deserialize(hexRLP);
        }

        private void Deserialize(byte[] rlp)
        {
            var data = RLP.Decode(rlp);

            Nonce = ByteManipulator.GetUInt32(data[0] ?? new byte[] { 0 });
            Ammount = new LargeInteger(data[1] ?? new byte[] { 0 });
            Fee = new LargeInteger(data[2] ?? new byte[] { 0 });
            Source = data[3] != null ? ByteManipulator.BigEndianTruncate(data[3], 33) : new byte[33];
            Destination = data[4] != null ? ByteManipulator.BigEndianTruncate(data[4], 33) : new byte[33];
            Signature = data[5] != null ? ByteManipulator.BigEndianTruncate(data[5], 64) : new byte[64];
            Network = data[6] ?? new byte[] { 0 };
        }

        public byte[] Serialize()
        {
            var tx = new byte[][]
            {
                ByteManipulator.GetBytes(Nonce),
                Ammount.GetBytes(),
                Fee.GetBytes(),
                Source,
                Destination,
                Signature,
                Network
            };

            return RLP.Encode(tx);
        }

        public void Sign(byte[] privateKey)
        {
            Signature = CryptographyHelper.GenerateSignatureSecp256k1(privateKey, SigningData());
        }

        public byte[] SigningData()
        {
            var hashTransaction = new Transaction()
            {
                Nonce = Nonce,
                Ammount = Ammount,
                Fee = Fee,
                Source = Source,
                Destination = Destination,
                Signature = null,
                Network = Network
            };

            return hashTransaction.Serialize();
        }

        public byte[] Hash()
        {
            return CryptographyHelper.Sha3256(Serialize());
        }

        public bool Verify()
        {
            return ArrayManipulator.Compare(Program.Network, Network) && CryptographyHelper.Secp256k1Verify(Source, Signature, SigningData());
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Nonce, Ammount, Fee, Source, Destination, Signature, Network);
        }

        public static bool operator ==(Transaction a, Transaction b)
        {
            return ArrayManipulator.Compare(a.Serialize(), b.Serialize());
        }

        public static bool operator !=(Transaction a, Transaction b)
        {
            return !ArrayManipulator.Compare(a.Serialize(), b.Serialize());
        }

        public override string ToString()
        {
            return 
            $"Nonce: {Nonce}\n" +
            $"Ammount: {Ammount / long.Parse(Math.Pow(10, 12).ToString())}\n" +
            $"Fee: {Fee / long.Parse(Math.Pow(10, 12).ToString())}\n" +
            $"Source: {HexConverter.ToPrefixString(Source)}\n" +
            $"Destination: {HexConverter.ToPrefixString(Destination)}\n";
        }
    }
}
 
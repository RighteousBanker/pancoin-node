using ByteOperation;
using Encoders;
using Panode.API;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Panode.Core
{
    public class BalanceLedger
    {
        LinearDictionaryEncoder balanceCoder = new LinearDictionaryEncoder("balance", 33, 8);
        LinearDictionaryEncoder nonceCoder = new LinearDictionaryEncoder("nonces", 33, 4);

        public BalanceLedger()
        {

        }

        public LargeInteger GetBalance(byte[] publicKey)
        {
            LargeInteger ret = null;
            var balance = balanceCoder.Get(publicKey);

            if (balance != null)
            {
                ret = new LargeInteger(balance);
            }

            return ret;
        }

        public Dictionary<byte[], Tuple<long, LargeInteger>> CreateDiff(List<Transaction> transactions, Direction direction, Dictionary<byte[], Tuple<long, LargeInteger>> initialDiff = null)
        {
            Dictionary<byte[], Tuple<long, LargeInteger>> ret;

            var nonces = new Dictionary<byte[], List<long>>(new ByteArrayComparer());
            var balances = new Dictionary<byte[], LargeInteger>(new ByteArrayComparer());

            if (initialDiff == null)
            {
                ret = new Dictionary<byte[], Tuple<long, LargeInteger>>(new ByteArrayComparer());
            }
            else
            {
                ret = initialDiff;
            }

            foreach (var kvp in ret)
            {
                balances.Add(kvp.Key, kvp.Value.Item2);
            }

            foreach (var transaction in transactions) //perform transactions
            {
                if (transaction.Verify())
                {
                    if (nonces.ContainsKey(transaction.Source))
                    {
                        nonces[transaction.Source].Add(transaction.Nonce);
                    }
                    else
                    {
                        nonces.Add(transaction.Source, new List<long>() { transaction.Nonce });
                    }

                    byte[] sourceBalanceBytes;
                    LargeInteger sourceBalance;
                    LargeInteger destinationBalance;

                    if (!balances.ContainsKey(transaction.Source))
                    {
                        sourceBalanceBytes = balanceCoder.Get(transaction.Source);

                        if (sourceBalanceBytes == null)
                        {
                            sourceBalanceBytes = new byte[] { 0 };
                        }

                        sourceBalance = new LargeInteger(sourceBalanceBytes);

                        balances.Add(transaction.Source, sourceBalance);
                    }
                    else
                    {
                        sourceBalance = balances[transaction.Source];
                    }

                    var totalAmmount = transaction.Ammount + transaction.Fee;

                    if (direction == Direction.Forward)
                    {
                        sourceBalance = sourceBalance - totalAmmount;
                    }
                    else
                    {
                        sourceBalance = sourceBalance + totalAmmount;
                    }

                    if (balances.ContainsKey(transaction.Destination))
                    {
                        destinationBalance = balances[transaction.Destination];

                        if (direction == Direction.Forward)
                        {
                            destinationBalance += transaction.Ammount;
                        }
                        else
                        {
                            destinationBalance -= transaction.Ammount;
                        }
                    }
                    else
                    {
                        if (balanceCoder.ContainsKey(transaction.Destination))
                        {
                            destinationBalance = new LargeInteger(balanceCoder.Get(transaction.Destination));

                            if (direction == Direction.Forward)
                            {
                                destinationBalance += transaction.Ammount;
                            }
                            else
                            {
                                destinationBalance -= transaction.Ammount;
                            }

                            balances.Add(transaction.Destination, destinationBalance);
                        }
                        else if (direction == Direction.Forward)
                        {
                            destinationBalance = transaction.Ammount;
                            balances.Add(transaction.Destination, destinationBalance);
                        }
                        else
                        {
                            ret = null;
                            destinationBalance = null;
                        }
                    }
                    
                    if (ret == null || sourceBalance == null || destinationBalance == null)
                    {
                        ret = null;
                    }
                    else
                    {
                        balances[transaction.Source] = sourceBalance;
                        balances[transaction.Destination] = destinationBalance;
                    }
                }
                else
                {
                    ret = null;
                    break;
                }
            }
            //check balance validity
            if (ret != null)
            {
                foreach (var kvp in balances)
                {
                    if (kvp.Value < 0)
                    {
                        ret = null;
                        break;
                    }
                }
            }
            //check nonce continuity
            if (ret != null)
            {
                foreach (var kvp in nonces)
                {
                    long startNonce = 0;

                    if (ret.ContainsKey(kvp.Key))
                    {
                        startNonce = ret[kvp.Key].Item1;
                    }
                    else
                    {
                        var nonceBytes = nonceCoder.Get(kvp.Key);

                        if (nonceBytes == null)
                        {
                            startNonce = -1;
                        }
                        else
                        {
                            startNonce = ByteManipulator.GetUInt32(nonceBytes);
                        }
                    }

                    kvp.Value.Sort();

                    if (direction == Direction.Reverse)
                    {
                        kvp.Value.Reverse();
                    }

                    var noncesCount = kvp.Value.Count;

                    for (int i = 0; i < noncesCount; i++)
                    {
                        if (direction == Direction.Forward)
                        {
                            if ((startNonce + i + 1) != kvp.Value[i])
                            {
                                ret = null;
                            }
                        }
                        else
                        {
                            long currentNonce = startNonce - i;

                            if (currentNonce != kvp.Value[0])
                            {
                                ret = null;
                            }
                            else
                            {
                                kvp.Value.Remove(currentNonce);

                                if (i == noncesCount - 1)
                                {
                                    kvp.Value.Add(currentNonce - 1);
                                }
                            }
                        }
                    }

                    if (ret == null)
                    {
                        break;
                    }
                }
            }

            //create new diff
            if (ret != null)
            {
                foreach (var kvp in balances)
                {
                    long nonce;
                    long initalNonce = -1; //-1 means that nonce does not exist -> nothing will be stored on disk, it is account which only recieved payments/mining reward

                    if (initialDiff.ContainsKey(kvp.Key))
                    {
                        initalNonce = initialDiff[kvp.Key].Item1;
                    }

                    if (nonces.ContainsKey(kvp.Key))
                    {
                        var nonceList = nonces[kvp.Key];

                        if (nonceList.Count == 0)
                        {
                            nonce = initalNonce;
                        }
                        else if (direction == Direction.Forward)
                        {
                            nonce = nonceList[nonceList.Count - 1];
                        }
                        else
                        {
                            nonce = nonceList[0];
                        }
                    }
                    else
                    {
                        nonce = initalNonce;
                    }

                    var record = new Tuple<long, LargeInteger>(nonce, kvp.Value);

                    if (ret.ContainsKey(kvp.Key))
                    {
                        ret[kvp.Key] = record;
                    }
                    else
                    {
                        ret.Add(kvp.Key, record);
                    }
                }
            }

            return ret;
        }

        public bool ApplyDiff(Dictionary<byte[], Tuple<long, LargeInteger>> diff)
        {
            var ret = true;

            foreach (var kvp in diff)
            {
                if (kvp.Value.Item2 < 0)
                {
                    ret = false;
                    break;
                }
            }

            if (ret)
            {
                foreach (var kvp in diff)
                {
                    //update balances
                    if (balanceCoder.ContainsKey(kvp.Key))
                    {
                        balanceCoder.Replace(kvp.Key, kvp.Value.Item2.GetBytes());
                    }
                    else
                    {
                        balanceCoder.Add(kvp.Key, kvp.Value.Item2.GetBytes());
                    }

                    //update nonces
                    if (nonceCoder.ContainsKey(kvp.Key))
                    {
                        if (kvp.Value.Item1 >= 0)
                        {
                            nonceCoder.Replace(kvp.Key, ByteManipulator.GetBytes((uint)kvp.Value.Item1));
                        }
                        else
                        {
                            nonceCoder.Remove(kvp.Key);
                        }
                    }
                    else
                    {
                        if (kvp.Value.Item1 >= 0)
                        {
                            nonceCoder.Add(kvp.Key, ByteManipulator.GetBytes((uint)kvp.Value.Item1));
                        }
                    }
                }
            }

            return ret;
        }

        public Dictionary<byte[], Tuple<long, LargeInteger>> RewardMiner(byte[] publicKey, Direction direction, Dictionary<byte[], Tuple<long, LargeInteger>> initialDiff = null)
        {
            Dictionary<byte[], Tuple<long, LargeInteger>> ret;

            if (initialDiff == null)
            {
                ret = new Dictionary<byte[], Tuple<long, LargeInteger>>(new ByteArrayComparer());
            }
            else
            {
                ret = initialDiff;
            }

            LargeInteger balance;
            long nonce;

            if (ret.ContainsKey(publicKey))
            {
                var record = ret[publicKey];
                nonce = record.Item1;
                balance = record.Item2;

                if (direction == Direction.Forward)
                {
                    balance = balance + Program.MinerReward;
                }
                else
                {
                    balance = balance - Program.MinerReward;
                }

                ret[publicKey] = new Tuple<long, LargeInteger>(nonce, balance);
            }
            else
            {
                var balanceBytes = balanceCoder.Get(publicKey);

                if (!((balanceBytes == null) && (direction == Direction.Reverse))) //balance is not found on disk and direction is reverse -> invalid reward
                {
                    var newBalance = new LargeInteger(balanceBytes);

                    if (direction == Direction.Forward)
                    {
                        newBalance = newBalance + Program.MinerReward;
                    }
                    else
                    {
                        newBalance = newBalance - Program.MinerReward;
                    }

                    if (newBalance < 0)
                    {
                        ret = null;
                    }
                    else
                    {
                        var nonceBytes = nonceCoder.Get(publicKey);

                        if (nonceBytes == null)
                        {
                            ret.Add(publicKey, new Tuple<long, LargeInteger>(-1, newBalance));
                        }
                        else
                        {
                            ret.Add(publicKey, new Tuple<long, LargeInteger>(ByteManipulator.GetUInt32(nonceBytes), newBalance));
                        }
                    }
                }
                else
                {
                    ret = null;
                }
            }

            return ret;
        }

        public uint GetTransactionCount(byte[] publicKey)
        {
            publicKey = ByteManipulator.BigEndianTruncate(publicKey, 33);
            var nonceBytes = nonceCoder.Get(publicKey);

            uint ret = 0;

            if (nonceBytes != null)
            {
                ret = ByteManipulator.GetUInt32(nonceBytes) + 1;
            }
            else
            {
                ret = 0;
            }

            return ret;
        }

        public void Dispose()
        {
            balanceCoder.Dispose();
            nonceCoder.Dispose();
        }
    }

    public enum Direction
    {
        Forward,
        Reverse
    }
}

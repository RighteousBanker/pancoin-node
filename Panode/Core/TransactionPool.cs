using ByteOperation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Panode.Core
{
    public class TransactionPool
    {
        readonly BalanceLedger _balanceLedger;

        Dictionary<string, Transaction> transactionByHash = new Dictionary<string, Transaction>();
        Dictionary<string, DateTime> dateByHash = new Dictionary<string, DateTime>();

        public int Count { get { return transactionByHash.Count; } }

        public TransactionPool(BalanceLedger balanceLedger)
        {
            _balanceLedger = balanceLedger;
        }

        public List<Transaction> GetUnknown(List<string> txHashes)
        {
            Clean();
            var ret = new List<Transaction>();

            foreach (var kvp in transactionByHash)
            {
                if (!txHashes.Contains(kvp.Key))
                {
                    ret.Add(kvp.Value);
                }
            }

            return ret;
        }

        public List<Transaction> GetTransactions(int count)
        {
            Clean();
            var ret = new List<Transaction>();

            count = count > transactionByHash.Count ? transactionByHash.Count : count;

            for (int i = 0; i < count; i++)
            {
                ret.Add(transactionByHash.Values.ToArray()[i]);
            }

            return ret;
        }

        public List<Transaction> GetMineableTransactions()
        {
            var ret = new List<Transaction>();

            var allTransactions = GetTransactions(Count);

            var txBySource = new Dictionary<string, List<Transaction>>();

            foreach (var tx in allTransactions)
            {
                var senderHex = HexConverter.ToPrefixString(tx.Source);

                if (txBySource.ContainsKey(senderHex))
                {
                    txBySource[senderHex].Add(tx);
                }
                else
                {
                    txBySource.Add(senderHex, new List<Transaction>() { tx });
                }
            }

            foreach (var kvp in txBySource)
            {
                uint nextNonce;

                lock (GateKeeper.BalanceLedgerLock)
                {
                    nextNonce = _balanceLedger.GetTransactionCount(kvp.Value[0].Source);
                }

                var sortedByNonce = kvp.Value.OrderBy(x => x.Nonce).ToList();

                for (int i = 0; i < sortedByNonce.Count; i++)
                {
                    if (sortedByNonce[i].Nonce == nextNonce)
                    {
                        ret.Add(sortedByNonce[i]);
                        nextNonce++;
                    }
                }
            }

            return ret;
        }

        public bool AddTransactions(List<Transaction> addedTransactions)
        {
            var ret = false;

            foreach (var transaction in addedTransactions)
            {
                var hash = HexConverter.ToPrefixString(transaction.Hash());

                if (!transactionByHash.ContainsKey(hash))
                {
                    if (VerifyTransaction(transaction))
                    {
                        transactionByHash.Add(hash, transaction);
                        dateByHash.Add(hash, DateTime.UtcNow);
                        ret = true;
                    }
                }
            }

            return ret;
        }

        public void Clean()
        {
            var transactionsToRemove = new List<string>();

            foreach (var kvp in transactionByHash)
            {
                if (!VerifyTransaction(kvp.Value))
                {
                    transactionsToRemove.Add(kvp.Key);
                }
            }

            foreach (var transaction in transactionsToRemove)
            {
                transactionByHash.Remove(transaction);
                dateByHash.Remove(transaction);
            }

            transactionsToRemove = new List<string>();

            foreach (var kvp in dateByHash)
            {
                if (kvp.Value < DateTime.UtcNow.AddHours(-1))
                {
                    transactionsToRemove.Add(kvp.Key);
                }
            }

            foreach (var transaction in transactionsToRemove)
            {
                transactionByHash.Remove(transaction);
                dateByHash.Remove(transaction);
            }
        }

        private bool VerifyTransaction(Transaction transaction)
        {
            var ret = false;

            if (transaction.Verify())
            {
                var nonce = _balanceLedger.GetTransactionCount(transaction.Source);

                if (transaction.Nonce >= nonce)
                {
                    var sourceBalance = _balanceLedger.GetBalance(transaction.Source);

                    if (sourceBalance != null)
                    {
                        if (sourceBalance + 1 > (transaction.Ammount + transaction.Fee))
                        {
                            ret = true;
                        }
                    }
                }
            }

            return ret;
        }
    }
}

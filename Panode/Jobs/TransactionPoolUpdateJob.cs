using ByteOperation;
using Panode.API;
using Panode.Core;
using Quartz;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Panode.Services
{
    [DisallowConcurrentExecution]
    public class TransactionPoolUpdateJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            var serviceProvider = (IServiceProvider)context.JobDetail.JobDataMap["ServiceProvider"];

            var contacts = (ContactLedger)serviceProvider.GetService(typeof(ContactLedger));

            var contact = contacts.GetRandomUrl();

            if (contact != null)
            {
                var provider = (HttpProvider)serviceProvider.GetService(typeof(HttpProvider));
                var txPool = (TransactionPool)serviceProvider.GetService(typeof(TransactionPool));

                List<Transaction> txs;

                lock (GateKeeper.TransactionPoolLock)
                {
                    lock (GateKeeper.BalanceLedgerLock)
                    {
                        txs = txPool.GetTransactions(txPool.Count);
                    }
                }

                var txHashes = new List<string>();

                foreach (var tx in txs)
                {
                    txHashes.Add(HexConverter.ToPrefixString(tx.Hash()));
                }

                var unknownTxs = await provider.PeerPost<List<string>, List<string>>(txHashes, contact + "transaction/unknown");

                if (unknownTxs != null)
                {
                    List<Transaction> deserializedTxs = new List<Transaction>();

                    try
                    {
                        foreach (var tx in unknownTxs)
                        {
                            deserializedTxs.Add(new Transaction(HexConverter.ToBytes(tx)));
                        }
                    }
                    catch
                    {
                        deserializedTxs = null;
                        Console.WriteLine($"[TransactionFetch] Transaction deserialization failed");
                    }

                    if (deserializedTxs != null)
                    {
                        lock (GateKeeper.TransactionPoolLock)
                        {
                            lock (GateKeeper.BalanceLedgerLock)
                            {
                                txPool.AddTransactions(deserializedTxs);
                            }
                        }

                        Console.WriteLine($"[TransactionFetch] Recieved {deserializedTxs.Count} transactions");
                    }
                }
                else
                {
                    Console.WriteLine($"[TransactionFetch] Request failed");
                }
            }
            else
            {
                Console.WriteLine($"[TransactionFetch] No contact");
            }
        }
    }
}

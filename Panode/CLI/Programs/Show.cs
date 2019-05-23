using Panode.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace Panode.CLI.Programs
{
    public class Show : IProgram
    {
        public object Run(IServiceProvider serviceProvider, string[] args)
        {
            if (args.Length > 0)
            {
                switch (args[0])
                {
                    case "block":
                        lock (GateKeeper.ChainManagerLock)
                        {
                            lock (GateKeeper.BalanceLedgerLock)
                            {
                                var chainManager = (ChainManager)serviceProvider.GetService(typeof(ChainManager));

                                if (args.Length == 2 && args[1] == "top")
                                {
                                    return chainManager.GetBlock((int)chainManager.Height).ToString();
                                }
                                else if (args.Length == 2)
                                {
                                    return chainManager.GetBlock(int.Parse(args[1])).ToString();
                                }
                            }
                        }
                        break;

                    case "contacts":

                        lock (GateKeeper.ContactLock)
                        {
                            var contacts = (ContactLedger)serviceProvider.GetService(typeof(ContactLedger));

                            foreach (var contact in contacts.GetAll())
                            {
                                Console.WriteLine(contact);
                            }
                        }

                        break;

                    case "txpool":

                        var txPool = (TransactionPool)serviceProvider.GetService(typeof(TransactionPool));

                        List<Transaction> transactions;

                        lock (GateKeeper.TransactionPoolLock)
                        {
                            lock (GateKeeper.BalanceLedgerLock)
                            {
                                if (args.Length > 1)
                                {
                                    transactions = txPool.GetTransactions(int.Parse(args[1]));
                                }
                                else
                                {
                                    transactions = txPool.GetTransactions(100);
                                }
                            }
                        }

                        var stringBuilder = new StringBuilder();

                        for (int i = 0; i < transactions.Count; i++)
                        {
                            stringBuilder.Append($"[Transaction {i + 1}]\n");
                            stringBuilder.Append(transactions[i].ToString());
                            stringBuilder.Append("\n");
                        }

                        Console.WriteLine(stringBuilder.ToString());

                        break;

                    case "work":

                        var miner = (Miner)serviceProvider.GetService(typeof(Miner));

                        Console.WriteLine(miner.GetUnminedBlock().ToString());

                        break;
                }
            }

            return null;
        }
    }
}

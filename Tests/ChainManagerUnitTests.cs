using ByteOperation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Panode;
using Panode.API;
using Panode.Core;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Tests
{
    [TestClass]
    public class ChainManagerUnitTests
    {
        [TestMethod]
        public void ProcessTransactions()
        {
            Program.CheckFolderStructure();
            Program.Settings = new Settings("settings.json");

            if (File.Exists("balance"))
            {
                File.Delete("balance");
            }
            if (File.Exists("nonces"))
            {
                File.Delete("nonces");
            }

            foreach (var file in Directory.GetFiles("chains"))
            {
                File.Delete(file);
            }

            foreach (var file in Directory.GetFiles("tables"))
            {
                File.Delete(file);
            }

            var walletA = new Wallet();
            var walletB = new Wallet();
            var walletC = new Wallet();
            var walletD = new Wallet();
            var walletE = new Wallet();
            var walletF = new Wallet();

            var walletM = new Wallet(); //miner

            var balanceLedger = new BalanceLedger();
            var manager = new ChainManager(balanceLedger);
            var contacts = new ContactLedger(new HttpProvider());

            //generate first blocks

            var minedBlocks = new List<Block>();

            minedBlocks.Add(CreateNextBlock(Block.Genesis, null, walletA.PublicKey));
            minedBlocks.Add(CreateNextBlock(minedBlocks[0], null, walletA.PublicKey));
            minedBlocks.Add(CreateNextBlock(minedBlocks[1], null, walletA.PublicKey));

            minedBlocks.Add(CreateNextBlock(minedBlocks[2], null, walletB.PublicKey));
            minedBlocks.Add(CreateNextBlock(minedBlocks[3], null, walletB.PublicKey));
            minedBlocks.Add(CreateNextBlock(minedBlocks[4], null, walletB.PublicKey));
            minedBlocks.Add(CreateNextBlock(minedBlocks[5], null, walletB.PublicKey));

            manager.ProcessBlocks(minedBlocks);

            var transactions = new List<Transaction>()
            {
                CreateTransaction(walletA, walletD, 100, 0),
                CreateTransaction(walletB, walletF, 200, 0),
                CreateTransaction(walletA, walletB, 400, 1),
                CreateTransaction(walletB, walletD, 800, 1),
                CreateTransaction(walletA, walletD, 1600, 2)
            };

            var txBlock = CreateNextBlock(minedBlocks[minedBlocks.Count - 1], transactions);

            manager.ProcessBlocks(new List<Block>()
            {
                txBlock
            });

            Assert.IsTrue(manager.GetBalance(walletA.PublicKey) == (Program.MinerReward * 3 - 100 - 400 - 1600 - 60));
            Assert.IsTrue(manager.GetBalance(walletB.PublicKey) == (Program.MinerReward * 4 - 200 - 800 - 40 + 400));

            Assert.IsTrue(manager.GetTransactionCount(walletA.PublicKey) == 3);
            Assert.IsTrue(manager.GetTransactionCount(walletB.PublicKey) == 2);

            var paddingBlocks = new List<Block>();

            paddingBlocks.Add(CreateNextBlock(txBlock, new List<Transaction>()
            {
                CreateTransaction(walletB, walletF, 3200, 2)
            }, walletA.PublicKey));

            paddingBlocks.Add(CreateNextBlock(paddingBlocks[0], new List<Transaction>()
            {
                CreateTransaction(walletA, walletE, 6400, 3)
            }, walletA.PublicKey));

            paddingBlocks.Add(CreateNextBlock(paddingBlocks[1], new List<Transaction>()
            {
                CreateTransaction(walletA, walletB, 12800, 4),
                CreateTransaction(walletD, walletC, 2400, 0)
            }, walletA.PublicKey));

            manager.ProcessBlocks(paddingBlocks);

            Assert.IsTrue(manager.GetBalance(walletA.PublicKey) == (Program.MinerReward * 6 - 100 - 400 - 1600 - 6400 - 12800 - 100));
            Assert.IsTrue(manager.GetBalance(walletB.PublicKey) == (Program.MinerReward * 4 - 200 - 800 - 3200 - 60 + 400 + 12800 ));
            Assert.IsTrue(manager.GetBalance(walletC.PublicKey) == 2400);
            Assert.IsTrue(manager.GetBalance(walletD.PublicKey) == 100 + 800 + 1600 - 2400 - 20);
            Assert.IsTrue(manager.GetBalance(walletE.PublicKey) == 6400);
            Assert.IsTrue(manager.GetBalance(walletF.PublicKey) == 200 + 3200);

            var forkBlocks = new List<Block>();

            forkBlocks.Add(CreateNextBlock(paddingBlocks[0], new List<Transaction>()
            {
                CreateTransaction(walletA, walletB, 2400, 3),
                CreateTransaction(walletD, walletC, 1200, 0)
            }, walletE.PublicKey));

            forkBlocks.Add(CreateNextBlock(forkBlocks[0], new List<Transaction>(), walletE.PublicKey));
            forkBlocks.Add(CreateNextBlock(forkBlocks[1], new List<Transaction>(), walletE.PublicKey));
            forkBlocks.Add(CreateNextBlock(forkBlocks[2], new List<Transaction>(), walletE.PublicKey));

            manager.ProcessBlocks(forkBlocks);

            Assert.IsTrue(manager.GetBalance(walletA.PublicKey) == (Program.MinerReward * 4 - 100 - 400 - 1600 -2400 - 80));
            Assert.IsTrue(manager.GetBalance(walletB.PublicKey) == (Program.MinerReward * 4 - 200 - 800 - 3200 - 60 + 400 + 2400));
            Assert.IsTrue(manager.GetBalance(walletC.PublicKey) == 1200);
            Assert.IsTrue(manager.GetBalance(walletD.PublicKey) == 100 + 800 + 1600 - 20 - 1200);
            Assert.IsTrue(manager.GetBalance(walletE.PublicKey) == Program.MinerReward * 4);
            Assert.IsTrue(manager.GetBalance(walletF.PublicKey) == 3400);

            var lowerNonceBlock = CreateNextBlock(forkBlocks.Last(), new List<Transaction>()
            {
                CreateTransaction(walletA, walletB, 2000, 2)
            });

            manager.ProcessBlocks(new List<Block>() { lowerNonceBlock });

            Assert.IsTrue(manager.Height == 13);

            var higherNonceBlock = CreateNextBlock(forkBlocks.Last(), new List<Transaction>()
            {
                CreateTransaction(walletA, walletB, 2000, 5)
            });

            manager.ProcessBlocks(new List<Block>() { higherNonceBlock});

            Assert.IsTrue(manager.Height == 13);

            var overBalanceBlock = CreateNextBlock(forkBlocks.Last(), new List<Transaction>()
            {
                CreateTransaction(walletF, walletB, 10000, 0)
            });

            manager.ProcessBlocks(new List<Block>() { overBalanceBlock });

            Assert.IsTrue(manager.Height == 13);

            var noBalanceBlock = CreateNextBlock(forkBlocks.Last(), new List<Transaction>()
            {
                CreateTransaction(new Wallet(), walletB, 10000, 0)
            });

            manager.ProcessBlocks(new List<Block>() { noBalanceBlock });

            Assert.IsTrue(manager.Height == 13);

            //persistency check
            manager.Dispose();

            balanceLedger = new BalanceLedger();
            manager = new ChainManager(balanceLedger);

            Assert.IsTrue(manager.GetBalance(walletA.PublicKey) == (Program.MinerReward * 4 - 100 - 400 - 1600 - 2400 - 80));
            Assert.IsTrue(manager.GetBalance(walletB.PublicKey) == (Program.MinerReward * 4 - 200 - 800 - 3200 - 60 + 400 + 2400));
            Assert.IsTrue(manager.GetBalance(walletC.PublicKey) == 1200);
            Assert.IsTrue(manager.GetBalance(walletD.PublicKey) == 100 + 800 + 1600 - 20 - 1200);
            Assert.IsTrue(manager.GetBalance(walletE.PublicKey) == Program.MinerReward * 4);
            Assert.IsTrue(manager.GetBalance(walletF.PublicKey) == 3400);

            //miner test
            var txPool = new TransactionPool(balanceLedger);

            Program.Settings.minerAddr = HexConverter.ToPrefixString(walletM.PublicKey);
            var miner = new Miner(balanceLedger, txPool, manager);
            
            for (int i = 0; i < 187; i++)
            {
                miner.UpdateParameters();
                while (miner.MineRound().Result == null) ;
            }

            miner.UpdateParameters();
            var ret = miner.MineRound().Result;

            Assert.IsTrue(manager.Height == 201);

            //tx pool test

            txPool.AddTransactions(new List<Transaction>()
            {
                CreateTransaction(walletA, walletF, 5000, manager.GetTransactionCount(walletA.PublicKey)),
                CreateTransaction(walletA, walletB, 5000, 5),
                CreateTransaction(walletA, walletB, 5000, 8)
            });

            var mineableTxs = txPool.GetMineableTransactions();
        }

        Block CreateNextBlock(Block previous, List<Transaction> transactions, byte[] minerAddr = null)
        {
            return new Block()
            {
                Difficulty = previous.Difficulty,
                Height = previous.Height + 1,
                Timestamp = previous.Timestamp + 120,
                MinerAddress = minerAddr ?? new byte[33],
                Nonce = new byte[32],
                PreviousBlockHash = previous.GetHash(),
                Transactions = transactions ?? new List<Transaction>()
            };
        }

        public static Transaction CreateTransaction(Wallet sender, Wallet recipient, LargeInteger value, uint nonce)
        {
            var ret = new Transaction()
            {
                Ammount = value,
                Destination = recipient.PublicKey,
                Network = Program.Network,
                Fee = 20,
                Nonce = nonce,
                Source = sender.PublicKey
            };

            ret.Sign(sender.PrivateKey);

            return ret;
        }
    }
}

using ByteOperation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Panode.Core
{
    public class Miner
    {
        readonly BalanceLedger _balanceLedger;
        readonly TransactionPool _transactionPool;
        readonly ChainManager _chainManager;

        int hashesPerRound = 100000;

        LargeInteger numberOfAttempts = 0;

        public LargeInteger NumberOfAttempts
        {
            get
            {
                var bytes = numberOfAttempts.GetBytes();
                numberOfAttempts = 0;
                return new LargeInteger(bytes);
            }
        }

        public uint BlockTime;
        uint lastBlockTimeStamp;
        int lastBlockHeight = 0;

        List<Transaction> transactions = new List<Transaction>();
        int height;
        uint timestamp;
        byte[] previousBlockHash;
        byte[] minerAddress;
        LargeInteger difficulty;
        LargeInteger nonce;

        public Miner(BalanceLedger balanceLedger, TransactionPool transactionPool,  ChainManager chainManager)
        {
            _balanceLedger = balanceLedger;
            _transactionPool = transactionPool;
            _chainManager = chainManager;

            var minerAddressBytes = HexConverter.ToBytes(Program.Settings.minerAddr);

            if (minerAddressBytes != null && minerAddressBytes.Length < 34)
            {
                minerAddress = ByteManipulator.BigEndianTruncate(minerAddressBytes, 33);
                Console.WriteLine($"[Miner] Miner initialized with address {HexConverter.ToPrefixString(minerAddress)}");
            }
            else
            {
                Console.WriteLine("[Miner] Warning: Invalid address. Miner initialized with address 0x0");
                minerAddress = new byte[33];
            }
        }

        public void UpdateParameters()
        {
            nonce = 0;
            timestamp = (uint)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

            lock (GateKeeper.TransactionPoolLock)
            {
                lock (GateKeeper.BalanceLedgerLock)
                {
                    transactions = _transactionPool.GetMineableTransactions();

                    if (transactions.Count > 100)
                    {
                        transactions = transactions.Take(100).ToList();
                    }
                }
            }

            lock (GateKeeper.ChainManagerLock)
            {
                var topBlock = _chainManager.GetBlock(_chainManager.Height);

                if (topBlock.Height > lastBlockHeight)
                {
                    numberOfAttempts = 0;
                    lastBlockHeight = topBlock.Height;
                }

                height = topBlock.Height + 1;
                previousBlockHash = topBlock.GetHash();

                lastBlockTimeStamp = topBlock.Timestamp;

                if (height == 1)
                {
                    difficulty = Block.LowestDifficulty;
                }
                else if (height % Program.DifficultyRecalibration == 1)
                {
                    var recalibrationBlock = _chainManager.GetBlock(height - Program.DifficultyRecalibration);
                    difficulty = ChainManager.CalculateDifficulty(recalibrationBlock, timestamp);
                }
                else
                {
                    difficulty = topBlock.Difficulty;
                }
            }
        }

        public async Task<Block> MineRound()
        {
            var block = new Block()
            {
                Difficulty = difficulty,
                Height = height,
                MinerAddress = minerAddress,
                PreviousBlockHash = previousBlockHash,
                Timestamp = timestamp,
                Transactions = transactions
            };

            var difficultyBytes = difficulty.GetBytes();

            for (int i = 0; i < hashesPerRound; i++)
            {
                block.Nonce = nonce.GetBytes();

                numberOfAttempts = numberOfAttempts + 1;

                if (ArrayManipulator.IsGreater(ByteManipulator.BigEndianTruncate(difficultyBytes, 32), block.GetHash(), difficultyBytes.Length)) //new block found
                {
                    lock (GateKeeper.ChainManagerLock)
                    {
                        _chainManager.ProcessBlocks(new List<Block>() { block });
                    }
                    lock (GateKeeper.TransactionPoolLock)
                    {
                        lock (GateKeeper.BalanceLedgerLock)
                        {
                            _transactionPool.Clean();
                        }
                    }

                    BlockTime = block.Timestamp - lastBlockTimeStamp;

                    return block;
                }

                nonce = nonce + 1;
            }

            return null;
        }

        public Block GetUnminedBlock()
        {
            UpdateParameters();

            return new Block()
            {
                Difficulty = difficulty,
                Height = height,
                PreviousBlockHash = previousBlockHash,
                Timestamp = timestamp,
                Transactions = transactions
            };
        }
    }
}

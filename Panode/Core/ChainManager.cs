using ByteOperation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;

namespace Panode.Core
{
    public class ChainManager //consensus layer
    {
        static readonly ulong maxFactor = 5000;
        static readonly ulong minFactor = 200;

        public int Height { get { return main.Height; } }

        BalanceLedger balanceLedger;

        Chain main = null;
        List<Chain> forks = new List<Chain>();
        
        public ChainManager(BalanceLedger ledger)
        {
            balanceLedger = ledger;
            var files = Directory.EnumerateFiles("chains");

            if (!File.Exists(@"chains\main"))
            {
                main = new Chain();
                main.AddBlock(Block.Genesis);
                balanceLedger.ApplyDiff(balanceLedger.RewardMiner(new byte[33], Direction.Forward));
            }
            else
            {
                foreach (var filename in files)
                {
                    if (filename == @"chains\main")
                    {
                        main = new Chain("main");
                    }
                    else
                    {
                        forks.Add(new Chain(filename.Split(@"\")[1]));
                    }
                }
            }
        }

        public void ProcessBlocks(List<Block> blocks)
        {
            var validBlocks = GetContinousValidBlocks(blocks);

            if (validBlocks.Count != 0)
            {
                CleanForks();

                var firstBlockChain = GetFork(validBlocks[0].PreviousBlockHash);

                if (firstBlockChain != null) //skip every block if first valid block does not continue in any chain
                {
                    var previousBlock = firstBlockChain.GetByHash(validBlocks[0].PreviousBlockHash);

                    if (previousBlock.Height == validBlocks[0].Height - 1)
                    {
                        for (int i = 0; i < validBlocks.Count; i++)
                        {
                            var fork = GetFork(validBlocks[i].GetHash(), firstBlockChain);

                            if (fork == null) //new block(s) :)
                            {
                                validBlocks = ArrayManipulator.SubArray(validBlocks.ToArray(), i, validBlocks.Count - i).ToList();
                                break;
                            }

                            firstBlockChain = fork;
                            previousBlock = validBlocks[i];
                        }

                        if (validBlocks.Count != 0)
                        {
                            var newBlocks = new List<Block>();

                            if (ValidateDifficulty(validBlocks, firstBlockChain))
                            {
                                for (int i = 0; i < validBlocks.Count; i++)
                                {
                                    if (validBlocks[i].Height > previousBlock.Height)
                                    {
                                        newBlocks.Add(validBlocks[i]);
                                    }
                                }

                                var virtualForkBlocks = CreateVirtualFork(firstBlockChain.GetByHash(newBlocks[0].PreviousBlockHash), newBlocks);

                                var forkBlockHeight = virtualForkBlocks[0].Height;

                                var reverseBlocks = main.GetBlocks(forkBlockHeight, (int)(main.Height - forkBlockHeight + 1)); //0 is common block

                                var balanceDiff = ProcessBalanceDiff(Direction.Reverse, reverseBlocks);

                                if (balanceDiff != null)
                                {
                                    balanceDiff = ProcessBalanceDiff(Direction.Forward, virtualForkBlocks, balanceDiff);

                                    if (balanceDiff != null) //nonces and balances are ok
                                    {
                                        Chain fork;

                                        if (newBlocks[0].Height != firstBlockChain.Height + 1) //fork
                                        {
                                            fork = Fork(virtualForkBlocks);
                                        }
                                        else //block addition
                                        {
                                            fork = firstBlockChain;

                                            foreach (var block in newBlocks)
                                            {
                                                firstBlockChain.AddBlock(block);
                                            }
                                        }

                                        if (IsHeavyest(fork))
                                        {
                                            balanceLedger.ApplyDiff(balanceDiff);

                                            if (fork != main)
                                            {
                                                SetMain(fork);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public Chain Fork(List<Block> blocks)
        {
            var chain = new Chain(blocks[0], blocks[1]);

            for (int i = 2; i < blocks.Count; i++)
            {
                chain.AddBlock(blocks[i]);
            }

            forks.Add(chain);

            return chain;
        }

        public static LargeInteger CalculateDifficulty(Block baseBlock, uint newBlockTimestamp)
        {
            ulong avgBlockTime = (ulong)(newBlockTimestamp - baseBlock.Timestamp) / (uint)Program.DifficultyRecalibration;

            if (avgBlockTime == 0)
            {
                avgBlockTime = 1;
            }

            ulong ratio = (1000 * avgBlockTime) / (ulong)Program.BlockTime;

            if (ratio > maxFactor)
            {
                ratio = maxFactor;
            }
            if (ratio < minFactor)
            {
                ratio = minFactor;
            }

            var newDifficulty = (baseBlock.Difficulty * new LargeInteger(ratio)) / 1000;

            if (newDifficulty > Block.LowestDifficulty)
            {
                newDifficulty = Block.LowestDifficulty;
            }

            return newDifficulty;
        }

        public LargeInteger GetBalance(byte[] publicKey)
        {
            LargeInteger ret = 0;

            if ((publicKey == null) || (publicKey.Length > 33))
            {
                ret = null;
            }
            else
            {
                ret = balanceLedger.GetBalance(publicKey);
            }

            return ret;
        }

        public uint GetTransactionCount(byte[] publicKey) => balanceLedger.GetTransactionCount(publicKey);

        public Block GetBlock(byte[] hash)
        {
            hash = ByteManipulator.BigEndianTruncate(hash, 33);
            return main.GetByHash(hash);
        }

        public Block GetBlock(int height)
        {
            return main.GetByHeight(height);
        }

        public List<Block> GetBlocks(int height, int count)
        {
            return main.GetBlocks(height, count);
        }

        private List<Block> CreateVirtualFork(Block forkBlock, List<Block> blocks)
        {
            List<Block> ret;
            var forkedChain = GetFork(forkBlock.GetHash());

            if (forkedChain == main)
            {
                ret = new List<Block>();
                ret.Add(forkBlock);

                foreach (var block in blocks)
                {
                    ret.Add(block);
                }
            }
            else
            {
                var blockCount = forkBlock.Height - forkedChain.ForkBlockHeight;

                ret = forkedChain.GetBlocks(0, (int)blockCount);

                ret.Add(forkBlock);

                foreach (var block in blocks)
                {
                    ret.Add(block);
                }
            }

            return ret;
        }

        private void CleanForks()
        {
            var forkArray = forks.ToArray();

            for (int i = 0; i < forkArray.Length; i++)
            {
                if (forkArray[i].ForkBlockHeight + Program.ForkForget < main.Height)
                {
                    forkArray[i].Delete();
                    forks.Remove(forkArray[i]);
                }
            }
        }

        private List<Block> GetContinousValidBlocks(List<Block> blocks)
        {
            blocks = blocks.OrderBy(x => x.Height).ToList();

            var ret = new List<Block>();

            for (int i = 0; i < blocks.Count; i++) //block continuation
            {
                if (blocks[i].Height + Program.ForkForget > main.Height) //discards every block with height lower than height - 100 and validates blocks
                {
                    if (ret.Count == 0)
                    {
                        if (blocks[i].SingleVerify())
                        {
                            ret.Add(blocks[i]);
                        }
                    }
                    else
                    {
                        if (blocks[i].Verify(blocks[i - 1]))
                        {
                            ret.Add(blocks[i]);
                        }
                        else
                        {
                            ret = new List<Block>();
                            break;
                        }
                    }
                }
            }

            return ret;
        }

        private Chain GetFork(byte[] hash, Chain prediction = null)
        {
            Chain ret = null;

            if ((prediction != null) && (prediction.ContainsBlock(hash)))
            {
                ret = prediction;
            }
            else if (main.ContainsBlock(hash))
            {
                ret = main;
            }
            else
            {
                foreach (var chain in forks)
                {
                    if ((chain != prediction) && chain.ContainsBlock(hash))
                    {
                        ret = chain;
                        break;
                    }
                }
            }

            return ret;
        }
         
        private Dictionary<byte[], Tuple<long, LargeInteger>> ProcessBalanceDiff(Direction direction, List<Block> blocks, Dictionary<byte[], Tuple<long, LargeInteger>>  previousDiff = null)
        {
            var ret = previousDiff ?? new Dictionary<byte[], Tuple<long, LargeInteger>>(new ByteArrayComparer());
            var transactions = new List<Transaction>();

            foreach (var block in blocks)
            {
                foreach (var transaction in block.Transactions)
                {
                    transactions.Add(transaction);
                }
            }

            foreach (var block in blocks)
            {
                if (block.Height == 0)
                {
                    continue;
                }

                ret = balanceLedger.RewardMiner(block.MinerAddress, direction, ret);

                if (ret == null)
                {
                    break;
                }
            }

            if (ret != null)
            {
                ret = balanceLedger.CreateDiff(transactions, direction, ret);
            }

            return ret;
        }
        
        private bool ValidateDifficulty(List<Block> blocks, Chain originalChain)
        {
            var ret = true;

            var originalBlock = originalChain.GetByHash(blocks[0].PreviousBlockHash);
            var difficulty = originalBlock.Difficulty;

            var difficultyValidationBlocks = new List<Block>();
            difficultyValidationBlocks.Add(originalBlock);
            
            foreach (var block in blocks)
            {
                difficultyValidationBlocks.Add(block);
            }
            
            foreach (var block in difficultyValidationBlocks)
            {
                if (block.Height == 1)
                {
                    difficulty = Block.LowestDifficulty;
                }
                else if ((block.Height % Program.DifficultyRecalibration) == 1)
                {
                    var difficultyCalculationBlockHeight = block.Height - Program.DifficultyRecalibration;

                    Block difficultyCalculationBlock;

                    if (difficultyCalculationBlockHeight < originalChain.ForkBlockHeight)
                    {
                        difficultyCalculationBlock = main.GetByHeight(difficultyCalculationBlockHeight);
                    }
                    else
                    {
                        difficultyCalculationBlock = originalChain.GetByHeight(difficultyCalculationBlockHeight);
                    }

                    difficulty = CalculateDifficulty(difficultyCalculationBlock, block.Timestamp);
                }

                if (block.Difficulty != difficulty)
                {
                    ret = false;
                    break;
                }
            }

            return ret;
        }

        private bool IsHeavyest(Chain fork)
        {
            var ret = false;

            if (fork == main)
            {
                ret = true;
            }
            else
            {
                LargeInteger mainRelativeDifficulty = 0;
                LargeInteger forkRelativeDifficulty = 0;

                var forkBlockHeight = fork.ForkBlockHeight;

                for (long i = forkBlockHeight; i <= main.Height; i++)
                {
                    mainRelativeDifficulty = mainRelativeDifficulty + main.GetByHeight(i).Difficulty;
                }

                for (long i = forkBlockHeight; i <= fork.Height; i++)
                {
                    forkRelativeDifficulty = forkRelativeDifficulty + fork.GetByHeight(i).Difficulty;
                }

                ret = forkRelativeDifficulty > mainRelativeDifficulty;
            }

            return ret;
        }

        private void SetMain(Chain newMain)
        {
            foreach (var fork in forks)
            {
                if (fork.ForkBlockHeight > newMain.ForkBlockHeight)
                {
                    fork.Delete();
                    forks.Remove(fork);
                }
            }

            var oldMainBlocks = main.GetBlocks(newMain.ForkBlockHeight);

            Fork(oldMainBlocks);

            main.Cut(newMain.ForkBlockHeight + 1); //dont cut common block, thanks

            var blocks = newMain.GetBlocks(newMain.ForkBlockHeight + 1);

            foreach (var block in blocks)
            {
                main.AddBlock(block);
            }

            newMain.Delete();
            forks.Remove(newMain); //Happy end
        }

        public void Dispose()
        {
            foreach (var fork in forks)
            {
                fork.Dispose();
            }

            main.Dispose();
            balanceLedger.Dispose();
        }
    }
}

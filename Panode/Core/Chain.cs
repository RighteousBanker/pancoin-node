using ByteOperation;
using Encoders;
using System;
using System.Collections.Generic;

namespace Panode.Core
{
    public class Chain
    {
        public long ForkBlockHeight { get; private set; }
        public int Height { get; private set; }
        public byte[] ForkBlockHash { get; private set; }

        SerialDictionaryEncoder coder;
        long topBlockOffset;

        //Filename: main or [hex fork block height]-[hex block hash]
        //block hash of fork block height + 1

        public Chain() //creates main chain
        {
            coder = new SerialDictionaryEncoder($"chains\\main", $"tables\\main", 32, 3);
            ForkBlockHash = Block.Genesis.GetHash();
            ForkBlockHeight = 0;
        }

        public Chain(Block forkBlock, Block firstBlock) //creates generic chain
        {
            var forkBlockHash = forkBlock.GetHash();

            string name;
            var randomValueBuffer = new byte[8];

            new Random().NextBytes(randomValueBuffer);

            name = forkBlock.Height.ToString() + "-" + HexConverter.ToPrefixString(randomValueBuffer);

            coder = new SerialDictionaryEncoder($"chains\\{name}", $"tables\\{name}", 32, 3); //blockhash length, max data length = 2^(3*8) bytes

            if (coder.Count != 0)
            {
                throw new Exception("Chain already exists");
            }

            ForkBlockHeight = forkBlock.Height;
            ForkBlockHash = forkBlockHash;

            AddBlock(forkBlock);

            topBlockOffset = 0;

            if (firstBlock != null)
            {
                AddBlock(firstBlock);
                Height = firstBlock.Height;
            }
            else
            {
                Height = forkBlock.Height;
            }
        }

        public Chain(string name) //loads chain
        {
            coder = new SerialDictionaryEncoder($"chains\\{name}", $"tables\\{name}", 32, 3);
            topBlockOffset = coder.Lookup.GetIndexOffset(coder.Count - 1);

            var topBlock = new Block(coder.Lookup.ReadByOffset(topBlockOffset));
            var bottomBlock = new Block(coder.Lookup.ReadByOffset(0));

            Height = (int)topBlock.Height;
            ForkBlockHeight = (int)bottomBlock.Height;
            ForkBlockHash = bottomBlock.GetHash();
        }

        public void AddBlock(Block block)
        {
            coder.Add(block.GetHash(), block.Serialize());

            if (Height > 0)
            {
                topBlockOffset = coder.Lookup.NextOffset(topBlockOffset);
            }
            else
            {
                topBlockOffset = 0;
            }

            Height = block.Height;
        }

        public bool ContainsBlock(byte[] blockHash)
        {
            return coder.ContainsKey(blockHash);
        }

        public Block GetByHash(byte[] hash)
        {
            Block ret;
            var bytes = coder.Get(hash);

            if (bytes != null)
            {
                ret = new Block(bytes);
            }
            else
            {
                ret = null;
            }

            return ret;
        }

        public Block GetByHeight(long height)
        {
            return new Block(coder.Lookup.ReadByIndex((int)(height - ForkBlockHeight)));
        }

        public Block GetTopBlock()
        {
            return new Block(coder.Lookup.ReadByOffset(topBlockOffset));
        }

        public Block GetBottomBlock()
        {
            return new Block(coder.Lookup.ReadByOffset(0));
        }

        public List<Block> GetBlocks(long startHeight, int count = 0)
        {
            var ret = new List<Block>();
            var offset = coder.Lookup.GetIndexOffset((int)(startHeight - ForkBlockHeight));

            if (count == 0)
            {
                count = (int)(Height - startHeight + 1);
            }

            for (int i = 0; i < count; i++)
            {
                var block = new Block(coder.Lookup.ReadByOffset(offset));

                if (block != null)
                {
                    ret.Add(block);
                    offset = coder.Lookup.NextOffset(offset);
                }
                else
                {
                    break;
                }
            }

            return ret;
        }

        public void Cut(long height)
        {
            var count = Height - height;

            for (int i = 0; i <= count; i++)
            {
                coder.RemoveTopItem();
            }
        }

        public void Delete()
        {
            coder.Delete();
        }

        public void Dispose()
        {
            coder.Dispose();
        }
    }
}

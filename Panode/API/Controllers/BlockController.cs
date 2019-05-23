using Microsoft.AspNetCore.Mvc;
using System;
using Panode.API.DTO;
using System.Collections.Generic;
using System.Text;
using Panode.Core;
using ByteOperation;

namespace Panode.API.Controllers
{
    [Route("block")]
    [Controller]
    public class BlockController : CustomControllerBase
    {
        readonly ChainManager _chainManager;
        readonly ContactLedger _contactLedger;
        readonly Miner _miner;
        readonly HttpProvider _httpProvider;

        public BlockController(ChainManager chainManager, Miner miner, ContactLedger contactLedger, HttpProvider httpProvider)
        {
            _chainManager = chainManager;
            _miner = miner;
            _contactLedger = contactLedger;
            _httpProvider = httpProvider;
        }

        [HttpPost("sync")]
        public IActionResult GetBlocks([FromBody] ViewModel<string> viewModel)
        {
            if (viewModel != null && viewModel.Data != null)
            {
                var blockHash = viewModel.Data;

                lock (GateKeeper.ChainManagerLock) lock (GateKeeper.BalanceLedgerLock)
                    {
                        var startBlock = _chainManager.GetBlock(HexConverter.ToBytes(blockHash));

                        if (startBlock != null)
                        {
                            var topHeight = _chainManager.Height;

                            if (startBlock.Height < topHeight)
                            {
                                var blockCount = topHeight - startBlock.Height;

                                var blocks = _chainManager.GetBlocks(startBlock.Height + 1, blockCount > 100 ? 100 : blockCount);

                                var serializedBlocks = new List<string>();

                                foreach (var block in blocks)
                                {
                                    serializedBlocks.Add(HexConverter.ToPrefixString(block.Serialize()));
                                }

                                return Ok(new SyncDTO()
                                {
                                    Blocks = serializedBlocks,
                                    Synced = serializedBlocks.Count == 100 ? false : true
                                });
                            }
                            else
                            {
                                return Ok(new SyncDTO()
                                {
                                    Synced = true,
                                    Blocks = null
                                });
                            }
                        }
                        else
                        {
                            var blockCount = _chainManager.Height < 100 ? _chainManager.Height : 100;
                            var blocks = _chainManager.GetBlocks(_chainManager.Height - blockCount, blockCount);

                            var serializedBlocks = new List<string>();

                            foreach (var block in blocks)
                            {
                                serializedBlocks.Add(HexConverter.ToPrefixString(block.Serialize()));
                            }

                            return Ok(new SyncDTO()
                            {
                                Blocks = serializedBlocks,
                                Synced = true
                            });
                        }
                    }
            }
            else
            {
                return BadRequest();
            }
        }

        [HttpPost("new")]
        public IActionResult New([FromBody] ViewModel<string> viewModel)
        {
            IActionResult ret;

            if (viewModel != null && viewModel.Data != null)
            {
                var blockHex = viewModel.Data;

                lock (GateKeeper.BalanceLedgerLock)
                {
                    lock (GateKeeper.ChainManagerLock)
                    {
                        Block block = null;

                        try
                        {
                            var currentHeight = _chainManager.Height;

                            block = new Block(HexConverter.ToBytes(blockHex));
                            _chainManager.ProcessBlocks(new List<Block>()
                            {
                                block
                            });

                            if (currentHeight == block.Height - 1)
                            {
                                Console.WriteLine($"[ChainManager] Recieved new block. Height: {block.Height}");
                                ret = Ok("ok");
                            }
                            else
                            {
                                ret = BadRequest();
                            }
                        }
                        catch
                        {
                            ret = BadRequest();
                        }
                    }
                }
            }
            else
            {
                ret = BadRequest();
            }

            return ret;
        }

        [HttpPost("relay")]
        public IActionResult Relay([FromBody] ViewModel<string> viewModel)
        {
            IActionResult ret = null;

            if (viewModel != null && viewModel.Data != null)
            {
                var blockHex = viewModel.Data;

                var currentHeight = _chainManager.Height;

                Block newBlock = null;

                lock (GateKeeper.BalanceLedgerLock)
                {
                    lock (GateKeeper.ChainManagerLock)
                    {
                        newBlock = new Block(HexConverter.ToBytes(blockHex));
                        _chainManager.ProcessBlocks(new List<Block>()
                        {
                            newBlock
                        });
                    }
                }

                if (newBlock != null)
                {
                    if (currentHeight == newBlock.Height - 1)
                    {
                        List<string> contacts;

                        lock (GateKeeper.ContactLock)
                        {
                            contacts = _contactLedger.GetAll();
                        }

                        foreach (var contact in contacts)
                        {
                            _httpProvider.PeerPost<string, string>(blockHex, contact + "block/new");
                        }

                        Console.WriteLine($"[ChainManager] Recieved new block. Height: {newBlock.Height}. Relaying to {contacts.Count} nodes");

                        ret = Ok();
                    }
                    else
                    {
                        ret = BadRequest();
                    }
                }
                else
                {
                    ret = BadRequest();
                }
            }
            else
            {
                ret = BadRequest();
            }

            if (ret == null)
            {
                ret = BadRequest();
            }

            return ret;
        }

        [HttpPost("getwork")]
        public IActionResult GetWork()
        {
            var block = _miner.GetUnminedBlock();
            return Ok(HexConverter.ToPrefixString(block.Serialize()));
        }
    }
}
 
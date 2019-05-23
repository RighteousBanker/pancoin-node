using ByteOperation;
using Panode.API;
using Panode.API.DTO;
using Panode.Core;
using Quartz;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Panode.Services
{
    [DisallowConcurrentExecution]
    public class FetchBlocksJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                var serviceProvider = (IServiceProvider)context.JobDetail.JobDataMap["ServiceProvider"];

                var contacts = (ContactLedger)serviceProvider.GetService(typeof(ContactLedger));

                var contact = contacts.GetRandomUrl();

                if (contact != null)
                {
                    var provider = (HttpProvider)serviceProvider.GetService(typeof(HttpProvider));
                    var chainManager = (ChainManager)serviceProvider.GetService(typeof(ChainManager));

                    var synced = false;

                    while (!synced)
                    {
                        byte[] topBlockHash = null;

                        lock (GateKeeper.ChainManagerLock)
                        {
                            topBlockHash = chainManager.GetBlock(chainManager.Height).GetHash();
                        }

                        var sync = await provider.PeerPost<string, SyncDTO>(HexConverter.ToPrefixString(topBlockHash), contact + "block/sync");

                        if (sync != null)
                        {
                            var stopwatch = new Stopwatch();
                            int newHeight = 0;

                            var blocks = new List<Block>();

                            if (sync.Blocks != null && sync.Blocks.Count > 0)
                            {
                                foreach (var blockRlp in sync.Blocks)
                                {
                                    try
                                    {
                                        blocks.Add(new Block(HexConverter.ToBytes(blockRlp)));
                                    }
                                    catch
                                    {

                                    }
                                }

                                Console.WriteLine($"[BlockFetch] Processing {blocks.Count} blocks from {contact}");

                                stopwatch.Start();

                                lock (GateKeeper.ChainManagerLock) lock (GateKeeper.BalanceLedgerLock)
                                    {
                                        chainManager.ProcessBlocks(blocks);
                                        newHeight = chainManager.Height;
                                    }
                            }

                            stopwatch.Stop();

                            if (sync.Synced)
                            {
                                if (blocks.Count > 0)
                                {
                                    Console.WriteLine($"[BlockFetch] Synced. Block height: {newHeight}. Time elapsed: {stopwatch.Elapsed}");
                                }
                                else
                                {
                                    Console.WriteLine($"[BlockFetch] Synced");
                                }
                            }
                            else
                            {
                                Console.WriteLine($"[BlockFetch] Not synced. Block height: {newHeight}. Time elapsed: {stopwatch.Elapsed}");
                            }

                            synced = sync.Synced;
                        }
                        else
                        {
                            contacts.RemoveContact(contact);
                            Console.WriteLine($"[BlockFetch] Connection to {contact} failed");
                            break;
                        }
                    }
                }
                else
                {
                    Console.WriteLine("[BlockFetch] No contact found");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"[BlockFetch] Exception occured");
                Console.WriteLine($"[BlockFetch] Stack trace: {e.StackTrace}");
                Console.WriteLine($"[BlockFetch] Message: {e.Message}");
            }
        }
    }
}

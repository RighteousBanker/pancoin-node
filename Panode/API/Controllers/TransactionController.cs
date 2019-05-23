using ByteOperation;
using Microsoft.AspNetCore.Mvc;
using Panode.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace Panode.API.Controllers
{
    [Route("transaction")]
    public class TransactionController : CustomControllerBase
    {
        readonly TransactionPool _transactionPool;
        readonly BalanceLedger _balanceLedger;
        readonly ContactLedger _contactLedger;
        readonly HttpProvider _httpProvider;

        public TransactionController(TransactionPool transactionPool, BalanceLedger balanceLedger, HttpProvider httpProvider, ContactLedger contactLedger)
        {
            _transactionPool = transactionPool;
            _balanceLedger = balanceLedger;
            _httpProvider = httpProvider;
            _contactLedger = contactLedger;
        }

        [HttpPost("new")]
        public IActionResult New([FromBody] ViewModel<string> viewModel) //data are RLP encoded TX
        {
            if (viewModel != null && viewModel.Data != null)
            {
                lock (GateKeeper.TransactionPoolLock) lock (GateKeeper.BalanceLedgerLock)
                    {
                        try
                        {
                            var tx = new Transaction(viewModel.Data);
                            if (_transactionPool.AddTransactions(new List<Transaction> { tx }))
                            {
                                Console.WriteLine("[TransactionController] Recieved new transaction!");
                                return Ok("ok");
                            }
                            else
                            {
                                return BadRequest();
                            }
                        }
                        catch
                        {
                            return BadRequest();
                        }
                    }
            }
            else
            {
                return BadRequest();
            }
        }

        [HttpPost("relay")]
        public IActionResult Relay([FromBody] ViewModel<string> viewModel) //data are RLP encoded TX
        {
            if (viewModel != null && viewModel.Data != null)
            {
                lock (GateKeeper.TransactionPoolLock)
                {
                    lock (GateKeeper.BalanceLedgerLock)
                    {
                        try
                        {
                            var tx = new Transaction(viewModel.Data);
                            if (_transactionPool.AddTransactions(new List<Transaction> { tx }))
                            {
                                List<string> contacts;

                                lock (GateKeeper.ContactLock)
                                {
                                    contacts = _contactLedger.GetAll();
                                }

                                foreach (var contact in contacts)
                                {
                                    _httpProvider.PeerPost<string, string>(viewModel.Data, contact + "transaction/new");
                                }

                                Console.WriteLine($"[TransactionController] Recieved new transaction! Relayed to {contacts.Count} nodes");

                                return Ok("ok");
                            }
                            else
                            {
                                return BadRequest();
                            }
                        }
                        catch
                        {
                            return BadRequest();
                        }
                    }
                }
            }
            else
            {
                return BadRequest();
            }
        }

        [HttpPost("balance")]
        public IActionResult Balance([FromBody] ViewModel<string> viewModel) //data are address
        {
            IActionResult ret;

            if (viewModel != null && viewModel.Data != null)
            {
                var address = HexConverter.ToBytes(viewModel.Data);

                if (address != null)
                {
                    var balance = _balanceLedger.GetBalance(address);

                    if (balance != null)
                    {
                        ret = Ok(HexConverter.ToPrefixString(balance));
                    }
                    else
                    {
                        ret = Ok("0x0");
                    }
                }
                else
                {
                    return BadRequest();
                }
            }
            else
            {
                ret = BadRequest();
            }

            return ret;
        }

        [HttpPost("count")]
        public IActionResult TransactionCount([FromBody] ViewModel<string> viewModel) //data are address
        {
            IActionResult ret;

            if (viewModel != null && viewModel.Data != null)
            {
                var address = HexConverter.ToBytes(viewModel.Data);

                if (address != null)
                {
                    var nonce = _balanceLedger.GetTransactionCount(address);

                    return Ok(nonce);
                }
                else
                {
                    return BadRequest();
                }
            }
            else
            {
                ret = BadRequest();
            }

            return ret;
        }

        [HttpPost("unknown")]
        public IActionResult GetUnknownTransactions([FromBody] ViewModel<List<string>> viewModel) //data are list of tx hashes, returns list of RLP serialized txs
        {
            lock (GateKeeper.TransactionPoolLock)
            {
                lock (GateKeeper.BalanceLedgerLock)
                {
                    if (viewModel != null && viewModel.Data != null)
                    {
                        var unknownTxs = _transactionPool.GetUnknown(viewModel.Data);

                        var transactions = new List<string>();

                        foreach (var tx in unknownTxs)
                        {
                            transactions.Add(HexConverter.ToPrefixString(tx.Serialize()));
                        }

                        return Ok(transactions);
                    }
                    else
                    {
                        return BadRequest();
                    }
                }
            }
        }
    }
}
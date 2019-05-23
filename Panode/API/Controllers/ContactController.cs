using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Panode.API;
using Panode.Core;

namespace Panode.API.Controllers
{
    [Route("contact")]
    public class ContactController : CustomControllerBase
    {
        readonly ContactLedger _contactLedger;

        public ContactController(ContactLedger contactLedger)
        {
            _contactLedger = contactLedger;
        }

        [HttpPost("random")]
        public IActionResult Random()
        {
            lock (GateKeeper.ContactLock)
            {
                var contacts = new List<string>();

                for (int i = 0; i < 10; i++)
                {
                    var contact = _contactLedger.GetRandomUrl();

                    if (contact != null)
                    {
                        contacts.Add(contact);
                    }
                }

                var distinctContacts = new List<string>();

                foreach (var contact in contacts)
                {
                    if (!distinctContacts.Contains(contact))
                    {
                        distinctContacts.Add(contact);
                    }
                }

                return Ok(distinctContacts);
            }
        }

        [HttpPost("handshake")]
        public async Task<IActionResult> Handshake([FromBody] ViewModel<object> viewModel)
        {
            if (viewModel != null && viewModel.IsAccessible && viewModel.Hostname != null && !viewModel.Hostname.Contains("127.0.0.1"))
            {
                Task.Run(() =>
                {
                    Thread.Sleep(2000);

                    if (_contactLedger.AddContact(viewModel.Hostname).Result)
                    {
                        Console.WriteLine($"[ContactController] Added {viewModel.Hostname} to contacts");
                    }
                });
            }

            return Ok("ok");
        }
    }
}

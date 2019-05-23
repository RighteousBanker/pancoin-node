using Panode.API;
using Panode.Core;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Panode.Services.ServiceJobs
{
    [DisallowConcurrentExecution]
    public class FetchContactsJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            var serviceProvider = (IServiceProvider)context.JobDetail.JobDataMap["ServiceProvider"];

            var contacts = (ContactLedger)serviceProvider.GetService(typeof(ContactLedger));

            var contact = contacts.GetRandomUrl();

            if (contact != null)
            {
                var provider = (HttpProvider)serviceProvider.GetService(typeof(HttpProvider));
                var newContacts = await provider.PeerPost<string, List<string>>("", contact + "contact/random");

                if (newContacts != null)
                {
                    var newContactsCount = 0;

                    foreach (var newContact in newContacts)
                    {
                        lock (GateKeeper.ContactLock)
                        {
                            if (contacts.AddContact(newContact).Result)
                            {
                                newContactsCount++;
                            }
                        }
                    }

                    if (newContactsCount > 0)
                    {
                        Console.WriteLine($"[ContactFetch] Found {newContactsCount} new contacts!");
                    }
                    else
                    {
                        Console.WriteLine($"[ContactFetch] No new contacts found");
                    }
                }
            }
            else
            {
                Console.WriteLine($"[ContactFetch] No contact found");
            }
        }
    }
}

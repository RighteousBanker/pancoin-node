using Panode.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace Panode.CLI.Programs
{
    public class Add : IProgram
    {
        public object Run(IServiceProvider serviceProvider, string[] args)
        {
            if (args.Length == 2)
            {
                switch (args[0])
                {
                    case "contact":
                        lock (GateKeeper.ContactLock)
                        {
                            var contactLedger = (ContactLedger)serviceProvider.GetService(typeof(ContactLedger));

                            if (contactLedger.AddContact(args[1]).Result)
                            {
                                Console.WriteLine("Contact added");
                            }
                            else
                            {
                                Console.WriteLine("Contact is inactive");
                            }
                        }

                        break;
                }
            }

            return null;
        }
    }
}

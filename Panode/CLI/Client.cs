using Newtonsoft.Json;
using Panode.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Panode.CLI
{
    public class Client
    {
        readonly IServiceProvider _serviceProvider;

        Dictionary<string, Type> programs = new Dictionary<string, Type>();

        public Client(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            var assembly = Assembly.Load("Panode");
            var types = assembly.GetTypes();

            var contactLedger = (ContactLedger)_serviceProvider.GetService(typeof(ContactLedger));

            Console.WriteLine($"[Contact ledger] Accessible nodes: {contactLedger.ContactCount}");

            foreach (var type in types)
            {
                if (type.GetInterfaces().ToList().Contains(typeof(IProgram)))
                {
                    programs.Add(type.Name.ToLower(), type);
                }
            }
        }

        public void Listen(string input)
        {
            var splitedInput = input.Split(' ');

            var programName = splitedInput[0].ToLowerInvariant();

            if (programs.ContainsKey(programName))
            {
                var program = (IProgram)Activator.CreateInstance(programs[programName]);
                var ret = program.Run(_serviceProvider, splitedInput.Skip(1).ToArray());

                if (ret is string)
                {
                    Console.Write((string)ret);
                }
                else if (ret != null)
                {
                    Console.WriteLine(JsonConvert.SerializeObject(ret));
                }
            }
            else
            {
                Console.WriteLine("Program was not found");
            }
        }
    }
}

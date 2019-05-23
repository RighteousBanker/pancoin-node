using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using ByteOperation;
using Microsoft.AspNetCore.Hosting;
using Panode.API;
using Panode.CLI;
using Quartz;
using Quartz.Impl;

namespace Panode
{
    public class Program
    {
        public static readonly int BlockTime = 180; //seconds 
        public static readonly int BlockSizeLimit = (int)Math.Pow(2, 20); //1 MB block
        public static readonly int ForkForget = 100; //deletes and ignores all forks forking below TopBlockHeight - ForkForget
        public static readonly int DifficultyRecalibration = 200; //number of blocks 
        public static readonly LargeInteger MinerReward = new LargeInteger(10000000000000);
        public static readonly byte[] Network = new byte[] { 1 };

        public static readonly string Version = "0.1";
        public static Settings Settings;

        public static IScheduler Scheduler; //Do not remove. GC will eat scheduler
        public static Task MinerTask;

        public static readonly List<string> CompatibleVersions = new List<string>()
        {
            "0.1"
        };

        static void Main(string[] args)
        {
            Settings = new Settings("settings.json");

            var consoleOutput = Console.Out;
            IServiceProvider serviceProvider = null;

            try
            {
                CheckFolderStructure();
                serviceProvider = StartKestrel();
            }
            catch (SocketException ex)
            {
                Settings.mine = false;
                Console.SetOut(consoleOutput);
                Console.WriteLine("[Node] Startup failed...");
                Console.WriteLine("[Node] SocketException: Check url in settings.json");
                Console.ReadKey();
                return;
            }
            catch (Exception ex)
            {
                Settings.mine = false;
                Console.SetOut(consoleOutput);
                Console.WriteLine("[Node] Startup failed...");
                Console.WriteLine($"[Node] {ex.GetType()}");
                Console.WriteLine($"[Node] {ex.Message}");
                Console.WriteLine($"[Node] {ex.StackTrace}");
                Console.ReadKey();
                return;
            }

            var cli = new Client(serviceProvider);

            while (true)
            {
                cli.Listen(Console.ReadLine());
            }
        }

        public static void CheckFolderStructure()
        {
            if (!Directory.Exists("chains"))
            {
                Directory.CreateDirectory("chains");
                Console.WriteLine("Directory chains created");
            }
            if (!Directory.Exists("tables"))
            {
                Directory.CreateDirectory("tables");
                Console.WriteLine("Directory tables created");
            }
        }

        static IServiceProvider StartKestrel()
        {
            var terminalOutput = Console.Out;

            Console.SetOut(new StreamWriter(Stream.Null));

            IWebHost host;

            string[] urls;

            if (Settings.accessible && Settings.url != null)
            {
                urls = new string[] { @"http://127.0.0.1:8125/", Settings.url };
            }
            else
            {
                urls = new string[] { @"http://127.0.0.1:8125/" };
                Settings.accessible = false;
            }

            host = new WebHostBuilder()
            .UseStartup<Startup>()
            .UseUrls(urls)
            .UseKestrel()
            .Build();

            host.Start();

            Console.SetOut(terminalOutput);

            Console.WriteLine("Node initialized");

            if (Settings.accessible)
            {
                Console.WriteLine($"Kestrel is listening at {Settings.url}");
            }

            return host.Services;
        }
    }
}

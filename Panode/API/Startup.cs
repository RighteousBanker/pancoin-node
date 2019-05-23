using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Panode.Services;
using Panode.Core;
using Panode.Services.ServiceJobs;
using System;
using Newtonsoft.Json.Serialization;
using Microsoft.Net.Http.Headers;
using Quartz;
using System.Threading.Tasks;
using Quartz.Impl;
using System.Collections.Generic;
using ByteOperation;

namespace Panode.API
{
    public class Startup
    {
        readonly string everyMinuteCron = "0 0/1 * 1/1 * ? *";

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            var mvcCore = services.AddMvcCore();
            mvcCore.AddJsonFormatters(options => options.ContractResolver = new CamelCasePropertyNamesContractResolver());

            services.AddSingleton(services);

            var httpProvider = new HttpProvider();

            services.AddSingleton(httpProvider);

            services.AddSingleton(new ContactLedger(httpProvider));

            var balanceLedger = new BalanceLedger();

            services.AddSingleton(balanceLedger);

            var chainManager = new ChainManager(balanceLedger);

            services.AddSingleton(chainManager);

            var txPool = new TransactionPool(balanceLedger);

            services.AddSingleton(txPool);
            
            services.AddSingleton(new Miner(balanceLedger, txPool, chainManager));

            var jobs = new List<Tuple<Type, string>>()
            {
                new Tuple<Type, string>(typeof(FetchContactsJob), everyMinuteCron),
                new Tuple<Type, string>(typeof(FetchBlocksJob), everyMinuteCron),
                new Tuple<Type, string>(typeof(TransactionPoolUpdateJob), everyMinuteCron),
            };

            var serviceProvider = services.BuildServiceProvider();

            ScheduleJobs(jobs, serviceProvider);

            if (Program.Settings.mine)
            {
                Program.MinerTask = Task.Run(() => StartMiner(serviceProvider));
            }
        }

        public void Configure(IApplicationBuilder app, Microsoft.AspNetCore.Hosting.IHostingEnvironment env)
        {
            app.UseContactLedgerMiddleware();

            app.UseMvc();
        }

        async Task ScheduleJobs(List<Tuple<Type, string>> jobs, IServiceProvider serviceProvider)
        {
            ISchedulerFactory schedulerFactory = new StdSchedulerFactory();
            Program.Scheduler = await schedulerFactory.GetScheduler();

            foreach (var jobDescriptor in jobs)
            {
                IJobDetail job = JobBuilder.Create(jobDescriptor.Item1).Build();
                job.JobDataMap["ServiceProvider"] = serviceProvider;

                ITrigger jobTrigger = TriggerBuilder.Create().WithCronSchedule(jobDescriptor.Item2).WithIdentity(jobDescriptor.Item1.Name).Build();
                await Program.Scheduler.ScheduleJob(job, jobTrigger);
            }

            await Program.Scheduler.Start();
        }

        async Task StartMiner(IServiceProvider serviceProvider)
        {
            try
            {
                var miner = (Miner)serviceProvider.GetService(typeof(Miner));
                var contactLedger = (ContactLedger)serviceProvider.GetService(typeof(ContactLedger));
                var httpProvider = (HttpProvider)serviceProvider.GetService(typeof(HttpProvider));

                while (Program.Settings.mine)
                {
                    miner.UpdateParameters();
                    var block = await miner.MineRound();

                    if (block != null) //relay new block
                    {
                        var difficultyBytes = ByteManipulator.TruncateMostSignificatZeroBytes(block.Difficulty.GetBytes());

                        var firstDifficultyBytes = new byte[4];

                        for (int i = 0; i < firstDifficultyBytes.Length; i++)
                        {
                            firstDifficultyBytes[i] = difficultyBytes[i];
                        }

                        Console.WriteLine($"[Miner] Block found! Height: {block.Height}, Tx count: {block.Transactions.Count}, Difficulty: {HexConverter.ToPrefixString(firstDifficultyBytes)} {difficultyBytes.Length - firstDifficultyBytes.Length} B, Block time: {miner.BlockTime} s, Hashrate: {miner.NumberOfAttempts / ((miner.BlockTime == 0 ? 1 : miner.BlockTime) * 1000)} kH/s");

                        List<string> contacts = null;

                        lock (GateKeeper.ContactLock)
                        {
                            contacts = contactLedger.GetAll();
                        }

                        foreach (var contact in contacts)
                        {
                            httpProvider.PeerPost<string, object>(HexConverter.ToPrefixString(block.Serialize()), contact + "block/new");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"[Miner] Exception occured");
                Console.WriteLine($"[Miner] Stack trace: {e.StackTrace}");
                Console.WriteLine($"[Miner] Message: {e.Message}");
            }
        }
    }
}

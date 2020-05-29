using CommandLine;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Yakka
{
    class Program
    {
        public static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        private static async Task MainAsync(string[] args)
        {
            var cts = new CancellationTokenSource();
            await Parser.Default
                .ParseArguments<Options>(args)
                .WithParsedAsync(async o =>
                {
                    Console.WriteLine($"config file: {o.ConfigFilepath}");
                    var config = await Config.GetConfigFromFile(o.ConfigFilepath);
                    if (config == null)
                    {
                        throw new ArgumentException("Error reading configuration file. Result was null.");
                    }

                    if (!config.Concurrency.Phases.Any())
                    {
                        config.Concurrency.Phases = config.BuildStandardConcurrencyPhases();
                    }
                    if (!config.Throughput.Phases.Any())
                    {
                        config.Throughput.Phases = config.BuildStandardThroughputPhases();
                    }

                    var plugin = new Plugin(config);
                    var threadControl = new ThreadControl(config, plugin);
                    var app = new Application(threadControl);
                    await app.Run(cts.Token);
                });
        }
    }
}

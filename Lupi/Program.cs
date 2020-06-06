using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lupi.Configuration;
using Lupi.Listeners;

namespace Lupi
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
                    var config = await ConfigHelper.GetConfigFromFile(o.ConfigFilepath);
                    if (config == null)
                    {
                        throw new ArgumentException("Error reading configuration file. Result was null.");
                    }

                    if (!config.Throughput.Phases.Any())
                    {
                        config.Throughput.Phases = config.BuildStandardThroughputPhases();
                    }
                    if (!config.Concurrency.Phases.Any())
                    {
                        config.Concurrency.Phases = config.BuildStandardConcurrencyPhases();
                    }

                    var plugin = new Plugin(config);
                    var testResultPublisher = new TestResultPublisher(config);
                    var threadControl = new ThreadControl(config, plugin, testResultPublisher);
                    
                    var listenerSubscribers = new Dictionary<string, Action>
                    {
                        { "file", () => testResultPublisher.Subscribe(new FileListener(config)) },
                        { "console", () => testResultPublisher.Subscribe(new ConsoleListener()) },
                        { "statsd", () => testResultPublisher.Subscribe(new StatsdListener(config)) }
                    };
                    config.Listeners.ActiveListeners.ForEach(l => listenerSubscribers[l]());

                    var app = new Application(threadControl, testResultPublisher);
                    await app.Run(cts.Token);
                });
        }
    }
}

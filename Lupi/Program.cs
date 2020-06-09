using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lupi.Configuration;
using Lupi.Listeners;
using YamlDotNet.Serialization;

namespace Lupi
{
    class Program
    {
        private static string LupiAsciiArt =
@"
██╗     ██╗   ██╗██████╗ ██╗
██║     ██║   ██║██╔══██╗██║
██║     ██║   ██║██████╔╝██║
██║     ██║   ██║██╔═══╝ ██║
███████╗╚██████╔╝██║     ██║
╚══════╝ ╚═════╝ ╚═╝     ╚═╝
";
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
                    Console.WriteLine(LupiAsciiArt);
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
                    Console.WriteLine("========");
                    Console.WriteLine(" Config");
                    Console.WriteLine("========\n");
                    var serializer = new Serializer();
                    serializer.Serialize(Console.Out, config);
                    Console.WriteLine("\n");

                    var plugin = new Plugin(config);
                    var testResultPublisher = new TestResultPublisher(config);
                    var aggregator = new Aggregator(config);
                    var threadControl = new ThreadControl(config, plugin, testResultPublisher);

                    var consoleAggregatorListener = new ConsoleAggregatorListener();
                    var testListenerSubscribers = new Dictionary<string, Action>
                    {
                        { "file", () => testResultPublisher.Subscribe(new FileListener(config)) },
                        { "statsd", () => testResultPublisher.Subscribe(new StatsdListener(config)) },
                        { "console", () => testResultPublisher.Subscribe(aggregator) }
                    };
                    var aggregatorListenerSubscribers = new Dictionary<string, Action>
                    {
                        { "console", () => aggregator.Subscribe(consoleAggregatorListener) }
                    };
                    config.Listeners.ActiveListeners.ForEach(l => {
                        if (testListenerSubscribers.ContainsKey(l))
                        {
                            testListenerSubscribers[l]();
                        }
                        if (aggregatorListenerSubscribers.ContainsKey(l))
                        {
                            aggregatorListenerSubscribers[l]();
                        }
                    });

                    var app = new Application(threadControl, testResultPublisher, aggregator);
                    await app.Run(cts.Token);
                });
        }
    }
}

using CommandLine;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lupi.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

                    var configErrors = config.Validate();
                    if (configErrors.Any())
                    {
                        Console.WriteLine("There were configuration errors:");
                        Console.WriteLine(string.Join(Environment.NewLine, configErrors));
                        return;
                    }

                    Console.WriteLine("========");
                    Console.WriteLine(" Config");
                    Console.WriteLine("========\n");
                    var serializer = new Serializer();
                    serializer.Serialize(Console.Out, config);
                    Console.WriteLine("\n");
                    var serviceProvider = IoC.GetServiceProvider(config);
                    var app = serviceProvider.GetService<IApplication>();
                    await app.Run(cts.Token);
                });
        }
    }
}

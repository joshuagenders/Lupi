using CommandLine;
using Lupi.Configuration;
using Lupi.Core;
using Microsoft.Extensions.DependencyInjection;

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

                    if (config.Listeners.ActiveListeners.Any(l => l.Equals("console", StringComparison.InvariantCultureIgnoreCase)))
                    {
                        Console.WriteLine("=========");
                        Console.WriteLine(" Config");
                        Console.WriteLine("=========\n");
                        YamlHelper.SerializeConsoleOut(config);
                        Console.WriteLine("\n");
                    }

                    var serviceProvider = IoC.GetServiceProvider(config);
                    using (var app = serviceProvider.GetService<IApplication>()){
                        var result = await app.Run();
                        if (result != 0)
                        {
                            throw new Exception($"Non-zero return code: {result}");
                        }
                    }
                });
        }
    }
}

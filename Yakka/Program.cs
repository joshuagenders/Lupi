﻿using CommandLine;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

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
            //todo requests scenario support -> input config file as yaml/json
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

                    var threadControl = new ThreadControl(config);
                    var testAdapter = new Runner();
                    var threadAllocator = new ThreadAllocator(testAdapter, threadControl);
                    var app = new Application(threadAllocator, threadControl, config);
                    await app.Run(cts.Token);
                });
        }
    }
}

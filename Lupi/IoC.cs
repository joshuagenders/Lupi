using Lupi.Configuration;
using Lupi.Listeners;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lupi
{
    public class IoC
    {
        public static IServiceProvider GetServiceProvider(Config config)
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            var serviceCollection = new ServiceCollection()
                .AddSingleton(configuration)
                .AddLogging(builder => 
                    builder.AddSerilog(
                        new LoggerConfiguration()
                            .ReadFrom.Configuration(configuration)
                            .CreateLogger(), true))
                .AddSingleton(config)
                .AddSingleton<IThreadControl, ThreadControl>()
                .AddSingleton<IApplication, Application>()
                .AddSingleton<ITestResultPublisher, TestResultPublisher>()
                .AddSingleton<ISystemMetricsPublisher, SystemMetricsPublisher>()
                .AddTransient<IPlugin, Plugin>();

            var testResultPublisher = new TestResultPublisher(config);
            var aggregator = new Aggregator(config);
            var testListenerSubscribers = new Dictionary<string, Action>
            {
                { "file", () => testResultPublisher.Subscribe(new FileListener(config)) },
                { "statsd", () => testResultPublisher.Subscribe(new StatsdListener(config)) },
                { "console", () => testResultPublisher.Subscribe(aggregator) }
            };

            var consoleAggregatorListener = new ConsoleAggregatorListener(config);

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

            var exitSignal = new ExitSignal();
            if (config.ExitConditions.Any())
            {
                aggregator.Subscribe(new ExitConditionAggregatorListener(config, exitSignal));
            }
            serviceCollection
                .AddSingleton<ITestResultPublisher>(testResultPublisher)
                .AddSingleton<IAggregator>(aggregator)
                .AddSingleton<IExitSignal>(exitSignal);

            return serviceCollection.BuildServiceProvider();
        }
    }
}

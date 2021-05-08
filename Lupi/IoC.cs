using JustEat.StatsD;
using Lupi.Configuration;
using Lupi.Core;
using Lupi.Listeners;
using Lupi.Results;
using Lupi.Services;
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
                .AddSingleton<IApplication, Application>()
                .AddTransient<IPlugin, Plugin>()
                .AddSingleton<ITestRunner, TestRunner>()
                .AddSingleton<ITestResultPublisher, TestResultPublisher>()
                .AddSingleton<ISystemMetricsPublisher, SystemMetricsPublisher>()
                .AddSingleton<IHttpEventListener, HttpEventListener>()
                .AddSingleton<ITimeService, TimeService>()
                .AddSingleton<ISleepService, SleepService>()
                .AddSingleton<IStopwatchFactory, StopwatchFactory>()
                .AddTransient<IStatsDPublisher, StatsDPublisher>(f => {
                    if (config.Listeners.ActiveListeners.Contains("statsd")){
                        return new StatsDPublisher(new StatsDConfiguration {
                            Host = config.Listeners.Statsd.Host,
                            Port = config.Listeners.Statsd.Port,
                            Prefix = config.Listeners.Statsd.Prefix
                        });
                    }
                    return default(StatsDPublisher);
                });

            var testResultPublisher = new TestResultPublisher(config);
            var aggregator = new Aggregator(config);
            var testListenerSubscribers = new Dictionary<string, Action>
            {
                { "file", () => testResultPublisher.Subscribe(new FileListener(config)) },
                { "statsd", () => testResultPublisher.Subscribe(new StatsdListener(config, 
                    new StatsDPublisher (new StatsDConfiguration {
                        Host = config.Listeners.Statsd.Host,
                        Port = config.Listeners.Statsd.Port,
                        Prefix = config.Listeners.Statsd.Prefix
                    }))) 
                },
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

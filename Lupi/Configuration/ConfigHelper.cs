using System;
using System.Linq;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Lupi.Configuration
{
    public static class ConfigHelper
    {
        private static T IfNotDefaultElse<T>(T itemIfNotDefault, T elseItem, T defaultVal) =>
            !itemIfNotDefault?.Equals(defaultVal) ?? defaultVal != null 
                ? itemIfNotDefault 
                : elseItem;
        public static async Task<Config> GetConfigFromFile(string filepath)
        {
            var configText = await System.IO.File.ReadAllTextAsync(filepath);
            return await GetConfigFromString(configText);
        }

        public static Config MapBaseConfig(Config config, Config baseConfig)
        {
            var newConfig = new Config
            {
                Concurrency = new Concurrency
                {
                    HoldFor = IfNotDefaultElse(config.Concurrency.HoldFor, baseConfig.Concurrency.HoldFor, TimeSpan.Zero),
                    RampUp = IfNotDefaultElse(config.Concurrency.RampUp, baseConfig.Concurrency.RampUp, TimeSpan.Zero),
                    RampDown = IfNotDefaultElse(config.Concurrency.RampDown, baseConfig.Concurrency.RampDown, TimeSpan.Zero),
                    MaxThreads = IfNotDefaultElse(config.Concurrency.MaxThreads, baseConfig.Concurrency.MaxThreads, 300),
                    MinThreads = IfNotDefaultElse(config.Concurrency.MinThreads, baseConfig.Concurrency.MinThreads, 1),
                    OpenWorkload = IfNotDefaultElse(config.Concurrency.OpenWorkload, baseConfig.Concurrency.OpenWorkload, default),
                    Phases = config.Concurrency.Phases?.Any() ?? false ? config.Concurrency.Phases : baseConfig.Concurrency.Phases,
                    ThreadIdleKillTime = IfNotDefaultElse(config.Concurrency.ThreadIdleKillTime, baseConfig.Concurrency.ThreadIdleKillTime, TimeSpan.FromSeconds(5)),
                    Threads = IfNotDefaultElse(config.Concurrency.Threads, baseConfig.Concurrency.Threads, default)
                },
                Throughput = new Throughput
                {
                    Phases = config.Throughput.Phases?.Any() ?? false ? config.Throughput.Phases : baseConfig.Throughput.Phases,
                    HoldFor = IfNotDefaultElse(config.Throughput.HoldFor, baseConfig.Throughput.HoldFor, TimeSpan.Zero),
                    RampUp = IfNotDefaultElse(config.Throughput.RampUp, baseConfig.Throughput.RampUp, TimeSpan.Zero),
                    RampDown = IfNotDefaultElse(config.Throughput.RampDown, baseConfig.Throughput.RampDown, TimeSpan.Zero),
                    Iterations = IfNotDefaultElse(config.Throughput.Iterations, baseConfig.Throughput.Iterations, default),
                    ThinkTime = IfNotDefaultElse(config.Throughput.ThinkTime, baseConfig.Throughput.ThinkTime, TimeSpan.Zero),
                    Tps = IfNotDefaultElse(config.Throughput.Tps, baseConfig.Throughput.Tps, default)
                },
                Engine = new Engine
                {
                    CheckInterval = IfNotDefaultElse(config.Engine.CheckInterval, baseConfig.Engine.CheckInterval, TimeSpan.FromMilliseconds(180)),
                    ResultPublishingInterval = IfNotDefaultElse(config.Engine.ResultPublishingInterval, baseConfig.Engine.ResultPublishingInterval, TimeSpan.FromMilliseconds(500))
                },
                Listeners = new Listeners
                {
                    ActiveListeners = config.Listeners
                            .ActiveListeners.Union(baseConfig.Listeners.ActiveListeners)
                            .Distinct()
                            .ToList(),
                    File = new File
                    {
                        Path = IfNotDefaultElse(config.Listeners.File.Path, baseConfig.Listeners.File.Path, default),
                    },
                    Statsd = new Statsd
                    {
                        Prefix = IfNotDefaultElse(config.Listeners.Statsd.Prefix, baseConfig.Listeners.Statsd.Prefix, default),
                        Bucket = IfNotDefaultElse(config.Listeners.Statsd.Bucket, baseConfig.Listeners.Statsd.Bucket, default),
                        Host = IfNotDefaultElse(config.Listeners.Statsd.Host, baseConfig.Listeners.Statsd.Host, default),
                        Port = IfNotDefaultElse(config.Listeners.Statsd.Port, baseConfig.Listeners.Statsd.Port, default)
                    }
                },
                Test = new Test
                {
                    AssemblyPath = IfNotDefaultElse(config.Test.AssemblyPath, baseConfig.Test.AssemblyPath, default),
                    SetupClass = IfNotDefaultElse(config.Test.SetupClass, baseConfig.Test.SetupClass, default),
                    SetupMethod = IfNotDefaultElse(config.Test.SetupMethod, baseConfig.Test.SetupMethod, default),
                    SingleTestClassInstance = IfNotDefaultElse(config.Test.SingleTestClassInstance, baseConfig.Test.SingleTestClassInstance, default),
                    TeardownClass = IfNotDefaultElse(config.Test.TeardownClass, baseConfig.Test.TeardownClass, default),
                    TeardownMethod = IfNotDefaultElse(config.Test.TeardownMethod, baseConfig.Test.TeardownMethod, default),
                    TestClass = IfNotDefaultElse(config.Test.TestClass, baseConfig.Test.TestClass, default),
                    TestMethod = IfNotDefaultElse(config.Test.TestMethod, baseConfig.Test.TestMethod, default),
                }
            };
            return newConfig;
        }

        public static async Task<Config> GetConfigFromString(string configText)
        {
            var deserializer = new DeserializerBuilder()
                .WithTypeConverter(new TimeSpanTypeConverter())
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            var config = deserializer.Deserialize<Config>(configText);
            if (!string.IsNullOrWhiteSpace(config.BaseConfig))
            {
                var baseConfig = await GetConfigFromFile(config.BaseConfig);
                config = MapBaseConfig(config, baseConfig);
            }

            if (!config.Throughput.Phases.Any())
            {
                config.Throughput.Phases = config.BuildStandardThroughputPhases();
            }
            if (!config.Concurrency.Phases.Any())
            {
                config.Concurrency.Phases = config.BuildStandardConcurrencyPhases();
            }

            return config;
        }
    }
}

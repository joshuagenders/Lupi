using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Lupi.Configuration
{
    public static class ConfigHelper
    {
        private static T GetConfigValue<T>(Func<Config, T> selector, List<Config> configs, T defaultValue) =>
            configs
                .Select(selector)
                .Where(v => v?.Equals(defaultValue) ?? false)
                .FirstOrDefault() ?? defaultValue;
        
        private static readonly Regex _envVarRegex = new Regex(@"\${(?<varname>.+?)}",
              RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Multiline);

        private static string ReplaceEnvironmentVars(string value) => 
            _envVarRegex.Replace(value, 
                m => Environment.GetEnvironmentVariable(m.Groups["varname"].Value) ?? "${" + m.Groups["varname"].Value + "}");

        public static Config MergeConfigs(List<Config> configs) => 
            new Config
            {
                Concurrency = new Concurrency
                {
                    HoldFor = GetConfigValue(v => v.Concurrency.HoldFor, configs, TimeSpan.Zero),
                    RampUp = GetConfigValue(v => v.Concurrency.RampUp,configs, TimeSpan.Zero),
                    RampDown = GetConfigValue(v => v.Concurrency.RampDown, configs, TimeSpan.Zero),
                    MaxThreads = GetConfigValue(v => v.Concurrency.MaxThreads, configs, default),
                    MinThreads = GetConfigValue(v => v.Concurrency.MinThreads, configs, default),
                    OpenWorkload = GetConfigValue(v => v.Concurrency.OpenWorkload, configs, default),
                    Phases = GetConfigValue(v => v.Concurrency.Phases, configs, Array.Empty<ConcurrencyPhase>().ToList()),
                    ThreadIdleKillTime = GetConfigValue(v => v.Concurrency.ThreadIdleKillTime, configs, TimeSpan.FromSeconds(5)),
                    Threads = GetConfigValue(v => v.Concurrency.Threads, configs, default)
                },
                Throughput = new Throughput
                {
                    Phases = GetConfigValue(v => v.Throughput.Phases, configs, Array.Empty<Phase>().ToList()),
                    HoldFor = GetConfigValue(v => v.Throughput.HoldFor, configs, TimeSpan.Zero),
                    RampUp = GetConfigValue(v => v.Throughput.RampUp, configs, TimeSpan.Zero),
                    RampDown = GetConfigValue(v => v.Throughput.RampDown, configs,TimeSpan.Zero),
                    Iterations = GetConfigValue(v => v.Throughput.Iterations, configs, default),
                    ThinkTime = GetConfigValue(v => v.Throughput.ThinkTime, configs, TimeSpan.Zero),
                    Tps = GetConfigValue(v => v.Throughput.Tps, configs, default)
                },
                Engine = new Engine
                {
                    CheckInterval = GetConfigValue(v => v.Engine.CheckInterval, configs, TimeSpan.FromMilliseconds(180)),
                    ResultPublishingInterval = GetConfigValue(v => v.Engine.ResultPublishingInterval, configs, TimeSpan.FromMilliseconds(500))
                },
                Listeners = new Listeners
                {
                    ActiveListeners = configs.SelectMany(c => c.Listeners.ActiveListeners)
                            .Distinct()
                            .ToList(),
                    File = new File
                    {
                        Path = GetConfigValue(v => v.Listeners.File.Path, configs, default),
                        Format = GetConfigValue(v => v.Listeners.File.Format, configs, default)
                    },
                    Statsd = new Statsd
                    {
                        Prefix = GetConfigValue(v => v.Listeners.Statsd.Prefix, configs, default),
                        Bucket = GetConfigValue(v => v.Listeners.Statsd.Bucket, configs, default),
                        Host = GetConfigValue(v => v.Listeners.Statsd.Host, configs, default),
                        Port = GetConfigValue(v => v.Listeners.Statsd.Port, configs, default)
                    },
                    Console = new ConsoleConfig
                    {
                        Format = GetConfigValue(v => v.Listeners.Console.Format, configs, default)
                    }
                },
                Test = new Test
                {
                    AssemblyPath = GetConfigValue(v => v.Test.AssemblyPath, configs, default),
                    SetupClass = GetConfigValue(v => v.Test.SetupClass, configs, default),
                    SetupMethod = GetConfigValue(v => v.Test.SetupMethod, configs, default),
                    SingleTestClassInstance = GetConfigValue(v => v.Test.SingleTestClassInstance, configs, default),
                    TeardownClass = GetConfigValue(v => v.Test.TeardownClass, configs, default),
                    TeardownMethod = GetConfigValue(v => v.Test.TeardownMethod, configs, default),
                    TestClass = GetConfigValue(v => v.Test.TestClass, configs, default),
                    TestMethod = GetConfigValue(v => v.Test.TestMethod, configs, default),
                },
                ExitConditions = configs.SelectMany(c => c.ExitConditions).ToList()
            };

        public static async Task<Config> GetConfigFromFile(string configFilepath)
        {
            var configs = await GetConfigsFromFile(configFilepath);
            var config = MergeConfigs(configs);
            return Build(config, configFilepath);
        }

        public static async Task<List<Config>> GetConfigsFromFile(string configFilepath)
        {
            var configs = new List<Config>();
            Config config;
            var path = configFilepath;
            var paths = new List<string>();
            do 
            {
                var fullPath = Path.GetFullPath(path);
                if (paths.Contains(fullPath))
                {
                    //configuration already loaded
                    break;
                }
                else
                {
                    paths.Add(fullPath);
                }

                var file = await System.IO.File.ReadAllTextAsync(path);
                file = ReplaceEnvironmentVars(file);
                config = YamlHelper.Deserialize<Config>(file);
                configs.Add(config);
                if (!string.IsNullOrWhiteSpace(config.BaseConfig))
                {
                    path = Path.IsPathRooted(config.BaseConfig)
                        ? config.BaseConfig
                        : Path.Join(configFilepath, config.BaseConfig);
                }
                else
                {
                    break;
                }
            } 
            while (!string.IsNullOrWhiteSpace(config?.BaseConfig));
            return configs;
        }

        public static Config Build(Config config, string path)
        {
            if (!Path.IsPathRooted(config.Test.AssemblyPath))
            {
                config.Test.AssemblyPath = Path.Combine(path ?? "./", config.Test.AssemblyPath);
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

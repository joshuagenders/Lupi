﻿using System.IO;
using System.Text.RegularExpressions;

namespace Lupi.Configuration
{
    public static class ConfigHelper
    {
        private static T GetConfigValue<T>(Func<Config, T> selector, List<Config> configs, T defaultValue) =>
            configs
                .Select(selector)
                .Where(v => !v?.Equals(defaultValue) ?? false)
                .FirstOrDefault() ?? defaultValue;

        private static readonly Regex _envVarRegex = new Regex(@"\${(?<varname>.+?)}",
              RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Multiline);

        private static string ReplaceEnvironmentVars(string value) =>
            _envVarRegex.Replace(value,
                m => Environment.GetEnvironmentVariable(m.Groups["varname"].Value) ?? "${" + m.Groups["varname"].Value + "}");

        public static Config MergeConfigs(List<Config> configs) =>
            new Config
            {
                Scripting = new Scripting
                {
                    Globals = configs.SelectMany(c => c.Scripting.Globals).ToDictionary(k => k.Key, v => v.Value),
                    Scenario = GetConfigValue(v => v.Scripting.Scenario, configs, new()),
                    Scripts = configs.SelectMany(c => c.Scripting.Scripts).ToDictionary(k => k.Key, v => v.Value),
                    Teardown = configs.SelectMany(c => c.Scripting.Teardown).ToDictionary(k => k.Key, v => v.Value)
                },
                Concurrency = new Concurrency
                {
                    HoldFor = GetConfigValue(v => v.Concurrency.HoldFor, configs, TimeSpan.Zero),
                    RampUp = GetConfigValue(v => v.Concurrency.RampUp, configs, TimeSpan.Zero),
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
                    RampDown = GetConfigValue(v => v.Throughput.RampDown, configs, TimeSpan.Zero),
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
            var r = Build(config, configFilepath);
            YamlHelper.SerializeConsoleOut(r);
            return r;
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

                var file = await System.IO.File.ReadAllTextAsync(fullPath);
                file = ReplaceEnvironmentVars(file);
                config = YamlHelper.Deserialize<Config>(file);
                configs.Add(config);
                if (!string.IsNullOrWhiteSpace(config.BaseConfig))
                {
                    path = Path.IsPathRooted(config.BaseConfig)
                        ? config.BaseConfig
                        : Path.Join(Path.GetDirectoryName(configFilepath), config.BaseConfig);
                }
                else
                {
                    break;
                }
            }
            while (!string.IsNullOrWhiteSpace(config?.BaseConfig));
            return configs;
        }

        public static Config Build(Config config, string configPath)
        {
            if (!Path.IsPathRooted(config.Test.AssemblyPath) && !config.Scripting.Scripts.Any())
            {
                config.Test.AssemblyPath = Path.Join(Path.GetDirectoryName(configPath), config.Test.AssemblyPath);
            }

            if (config.Scripting.Scripts.Any())
            {
                var invalidSteps = config.Scripting.Scenario.Where(s => !config.Scripting.Scripts.ContainsKey(s));
                if (invalidSteps.Any())
                {
                    throw new ArgumentException($"Could not find scenario key(s) in scripting.scripts. {string.Join(", ", invalidSteps)}");
                }
                var scriptsToLoad = config.Scripting.Scripts
                    .Where(s => !string.IsNullOrWhiteSpace(s.Value.ScriptPath))
                    .Where(s => string.IsNullOrWhiteSpace(s.Value.Script));
                foreach (var script in config.Scripting.Scripts)
                {
                    if (scriptsToLoad.Contains(script))
                    {
                        var filePath = Path.IsPathRooted(script.Value.ScriptPath)
                            ? script.Value.ScriptPath
                            : Path.Join(Path.GetDirectoryName(configPath), script.Value.ScriptPath);
                        if (!System.IO.File.Exists(filePath))
                        {
                            Console.WriteLine($"File not found for script {script.Key} at '{script.Value.ScriptPath}'. Full path {filePath}");
                            throw new FileNotFoundException($"{script.Value}");
                        }
                        var fileContents = System.IO.File.ReadAllText(filePath);
                        script.Value.Script = fileContents;
                    }
                    var fullPathReferences = new List<string>();
                    foreach (var reference in script.Value.References ?? Enumerable.Empty<string>())
                    {
                        var referenceFilePath = Path.GetFullPath(Path.Join(Path.GetFullPath(configPath), reference));
                        if (Path.EndsInDirectorySeparator(referenceFilePath))
                        {
                            if (!System.IO.Directory.Exists(referenceFilePath))
                            {
                                Console.WriteLine($"Foldder not found for script {script.Key} reference '{reference}'. Full path '{referenceFilePath}'. Relative to {configPath}");
                                throw new FileNotFoundException($"{script.Value}");
                            }
                            fullPathReferences.AddRange(System.IO.Directory.GetFiles(referenceFilePath, "*.dll"));
                        }
                        else
                        {
                            if (!System.IO.File.Exists(referenceFilePath))
                            {
                                Console.WriteLine($"File not found for script {script.Key} reference '{reference}'. Full path '{referenceFilePath}'. Relative to {configPath}");
                                throw new FileNotFoundException($"{script.Value}");
                            }
                            fullPathReferences.Add(referenceFilePath);
                        }
                    }
                    script.Value.References = fullPathReferences;
                }
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

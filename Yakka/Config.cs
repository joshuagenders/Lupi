﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace Yakka
{
    public class Config
    {
        public Concurrency Concurrency { get; set; } = new Concurrency();
        public Throughput Throughput { get; set; } = new Throughput();
        public Test Test { get; set; } = new Test();
        public bool ThroughputEnabled => 
            Throughput != null 
            && (Throughput.Tps > 0 || Throughput.Phases.Any(p => p.ToTps > 0 || p.FromTps > 0 || p.Tps > 0));

        public static async Task<Config> GetConfigFromFile(string filepath)
        {
            var configText = await File.ReadAllTextAsync(filepath);
            var deserializer = new DeserializerBuilder()
                .WithTypeConverter(new TimeSpanTypeConverter())
                .Build();

            var config = deserializer.Deserialize<Config>(configText);
            if (!config.Throughput.Phases.Any())
            {
                config.Throughput.Phases = config.BuildStandardThroughputPhases();
            }
            return config;
        }
    }

    public class Concurrency
    {
        public int Threads { get; set; }
        public TimeSpan RampUp { get; set; }
        public TimeSpan RampDown { get; set; }
        public bool OpenWorkload { get; set; } // requires throughput
        public int MaxConcurrency { get; set; } //requires open workload
    }

    public class Throughput
    {
        public double Tps { get; set; }
        public TimeSpan RampUp { get; set; }
        public TimeSpan HoldFor { get; set; }
        public TimeSpan RampDown { get; set; }
        public List<Phase> Phases { get; set; } = new List<Phase>();
        public TimeSpan ThinkTime { get; set; }
        public int Iterations { get; set; }
    }

    public class Phase
    {
        public TimeSpan Duration { get; set; }
        public double Tps { get; set; }
        [YamlMember(Alias = "from")]
        public double FromTps { get; set; }
        [YamlMember(Alias = "to")]
        public double ToTps { get; set; }
    }

    public class Test {
        public string AssemblyPath { get; set; }
        public bool SingleTestClassInstance { get; set; }
        public string TestClass { get; set; }
        public string TestMethod { get; set; } //fully namespaced
        public string AssemblySetupMethod { get; set; } //fully namespaced, by default search for signature public static Startup(IContainerBuilder)
        public string AssemblyTeardownMethod { get; set; } //fully namespaced, by default do nothing
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Serialization;

namespace Lupi.Configuration
{
    public class Config
    {
        public Concurrency Concurrency { get; set; } = new Concurrency();
        public Throughput Throughput { get; set; } = new Throughput();
        public Test Test { get; set; } = new Test();

        public Engine Engine { get; set; } = new Engine();
        public Listeners Listeners { get; set; } = new Listeners();
        public string BaseConfig { get; set; }

        public bool ThroughputEnabled => 
            Throughput != null 
            && (Throughput.Tps > 0 || Throughput.Phases.Any(p => p.ToTps > 0 || p.FromTps > 0 || p.Tps > 0));
    }

    public class Concurrency
    {
        public int Threads { get; set; }
        public TimeSpan RampUp { get; set; }
        public TimeSpan HoldFor { get; set; }
        public TimeSpan RampDown { get; set; }
        public bool OpenWorkload { get; set; }
        public int MinThreads { get; set; } = 1;
        public int MaxThreads { get; set; } = 300;

        public List<ConcurrencyPhase> Phases { get; set; } = new List<ConcurrencyPhase>();
        public TimeSpan ThreadIdleKillTime { get; set; } = TimeSpan.FromSeconds(5);
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

    public class ConcurrencyPhase
    {
        public TimeSpan Duration { get; set; }
        public int Threads { get; set; }
        [YamlMember(Alias = "from")]
        public int FromThreads { get; set; }
        [YamlMember(Alias = "to")]
        public int ToThreads { get; set; }
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

    public class Test 
    {
        public string AssemblyPath { get; set; }
        public bool SingleTestClassInstance { get; set; }
        public string TestClass { get; set; }
        public string TestMethod { get; set; }
        public string SetupClass { get; set; }
        public string SetupMethod { get; set; } 
        public string TeardownClass { get; set; }
        public string TeardownMethod { get; set; }
    }

    public class Engine
    {
        public TimeSpan ResultPublishingInterval { get; set; } = TimeSpan.FromMilliseconds(500);
        public TimeSpan CheckInterval { get; set; } = TimeSpan.FromMilliseconds(220);
        public TimeSpan AggregationInterval { get; set; } = TimeSpan.FromSeconds(2);
    }

    public class Listeners
    {
        public List<string> ActiveListeners { get; set; } = new List<string>();
        public Statsd Statsd { get; set; } = new Statsd();
        public File File { get; set; } = new File();
        public ConsoleConfig Console { get; set; }
    }

    public class File
    {
        public string Path { get; set; }
        public string Format { get; set; }
    }

    public class ConsoleConfig
    {
        public string Format { get; set; }
    }

    public class Statsd
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string Prefix { get; set; }
        public string Bucket { get; set; }
    }
}

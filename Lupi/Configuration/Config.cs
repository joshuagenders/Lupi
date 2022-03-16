using YamlDotNet.Serialization;

namespace Lupi.Configuration
{
    public class Config
    {
        public Concurrency Concurrency { get; set; } = new();
        public Throughput Throughput { get; set; } = new();
        public Test Test { get; set; } = new();
        public List<ExitCondition> ExitConditions { get; set; } = new();

        public Engine Engine { get; set; } = new();
        public Listeners Listeners { get; set; } = new();
        public string BaseConfig { get; set; }

        public bool ThroughputEnabled =>
            Throughput != null
            && (Throughput.Tps > 0 || Throughput.Phases.Any(p => p.ToTps > 0 || p.FromTps > 0 || p.Tps > 0));

        public Scripting Scripting { get; set; } = new();
    }

    public class Concurrency
    {
        public int Threads { get; set; }
        public TimeSpan RampUp { get; set; }
        public TimeSpan HoldFor { get; set; }
        public TimeSpan RampDown { get; set; }
        public bool OpenWorkload { get; set; }
        public int MinThreads { get; set; }
        public int MaxThreads { get; set; }

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
        public ConsoleConfig Console { get; set; } = new ConsoleConfig();
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
    }

    public class ExitCondition
    {
        public string Property { get; set; }
        public string Operator { get; set; }
        public double Value { get; set; }
        public TimeSpan Duration { get; set; }
        public int Periods { get; set; }
        public string PassedFailed { get; set; }
    }

    public class Scripting
    {
        public Dictionary<string, LupiScript> Scripts { get; set; } = new();
        public Dictionary<string, LupiScript> Globals { get; set; } = new();
        public Dictionary<string, LupiScript> Teardown { get; set; } = new();
        public List<string> Scenario { get; set; } = new();
    }

    public class LupiScript
    {
        public string Script { get; set; }
        public string ScriptPath { get; set; }
        /// <summary>
        /// List of namespaces to import.
        /// </summary>
        public IEnumerable<string> Imports { get; set; } = Enumerable.Empty<string>();

        /// <summary>
        /// List of file paths referencing required DLLs.
        /// </summary>
        public IEnumerable<string> References { get; set; } = Enumerable.Empty<string>();
    }
}

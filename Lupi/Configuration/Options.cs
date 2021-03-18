using System;
using System.Collections.Generic;
using CommandLine;

namespace Lupi.Configuration
{
    public class Options
    {
        [Value(1, Required = true, HelpText = "The configuration file path")]
        public string ConfigFilepath { get; set; }

        [Option('f', "file", HelpText = "The filepath of the .DLL containing the test.")]
        public int TestFile { get; set; }

        [Option('t', "test", HelpText = "The fully qualified class and method name. E.g. 'MyNamespace.MyClassName::MyMethod'")]
        public int Test { get; set; }

        [Option('c', "concurrency", HelpText = "The number of concurrent tests.")]
        public int Concurrency { get; set; }

        [Option('r', "throughput", HelpText = "The number of tests per second.")] 
        public double Tps { get; set; }

        [Option('w', "ramp-up", HelpText = "Ramp-up seconds.")]
        public int RampUpMinutes { get; set; }

        [Option('d', "duration", HelpText = "Duration seconds.")]
        public int DurationSeconds { get; set; }

        [Option('s', "ramp-down", HelpText = "Ramp-down seconds.")]
        public int RampDownMinutes { get; set; }

        [Option('e', "exit-condition", HelpText = "Comma separated list of Exit condition(s).")]
        public IEnumerable<string> ExitConditions { get; set; }

    }
}

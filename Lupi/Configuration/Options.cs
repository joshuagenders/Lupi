using CommandLine;

namespace Lupi.Configuration
{
    public class Options
    {
        [Value(1, Required = true, HelpText = "The configuration file path")]
        public string ConfigFilepath { get; set; }
    }
}

using System.Linq;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Lupi.Configuration
{
    public static class ConfigHelper
    {
        public static async Task<Config> GetConfigFromFile(string filepath)
        {
            var configText = await System.IO.File.ReadAllTextAsync(filepath);
            return GetConfigFromString(configText);
        }

        public static Config GetConfigFromString(string configText)
        {
            var deserializer = new DeserializerBuilder()
                .WithTypeConverter(new TimeSpanTypeConverter())
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            var config = deserializer.Deserialize<Config>(configText);
            if (!config.Throughput.Phases.Any())
            {
                config.Throughput.Phases = config.BuildStandardThroughputPhases();
            }
            return config;
        }
    }
}

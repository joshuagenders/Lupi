using AutoMapper;
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
            return await GetConfigFromString(configText);
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
                var mapper = new Mapper(new MapperConfiguration(cfg => cfg
                    .CreateMap<Config, Config>()
                    .ForAllMembers(o => o.Condition((source, destination, member) => member != null))));
                config = mapper.Map(config, baseConfig);
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

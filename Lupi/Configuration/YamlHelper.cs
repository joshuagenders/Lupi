using Lupi.Configuration;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Lupi
{
    public static class YamlHelper
    {
        public static string Serialize<T>(T obj)
        {
            var serializer = new SerializerBuilder()
                .WithTypeConverter(new TimeSpanTypeConverter())
                .WithTypeConverter(new ExitConditionTypeConverter())
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            return serializer.Serialize(obj);
        }

        public static void SerializeConsoleOut<T>(T obj)
        {
            var serializer = new SerializerBuilder()
                .WithTypeConverter(new TimeSpanTypeConverter())
                .WithTypeConverter(new ExitConditionTypeConverter())
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            serializer.Serialize(Console.Out, obj);
        }

        public static T Deserialize<T>(string yaml) 
        {
            var deserializer = new DeserializerBuilder()
                .WithTypeConverter(new TimeSpanTypeConverter())
                .WithTypeConverter(new ExitConditionTypeConverter())
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            return deserializer.Deserialize<T>(yaml);
        }
    }
}

using System;
using System.Text.RegularExpressions;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Yakka
{
    public class TimeSpanTypeConverter : IYamlTypeConverter
    {
        private static Regex TimeStringRegex = new Regex(@"^(?!$)(?:(\d+)d)?(?:(\d+)h)?(?:(\d+)m)?(?:(\d+)s)?(?:(\d+)ms)?$", RegexOptions.Compiled);

        public bool Accepts(Type type)
        {
            return type.Equals(typeof(TimeSpan));
        }

        public object ReadYaml(IParser parser, Type type)
        {
            var value = parser.Consume<Scalar>().Value;
            if (string.IsNullOrWhiteSpace(value))
            {
                return TimeSpan.Zero;
            }
            var parsed = TimeStringRegex.Match(value);
            if (!parsed.Success)
            {
                return TimeSpan.Zero;
            }

            Func<string, int> ParseGroup = new Func<string, int>(s => string.IsNullOrWhiteSpace(s) ? 0 : int.Parse(s));

            var values = new
            {
                d = ParseGroup(parsed.Groups[1].Value),
                h = ParseGroup(parsed.Groups[2].Value),
                m = ParseGroup(parsed.Groups[3].Value),
                s = ParseGroup(parsed.Groups[4].Value),
                ms = ParseGroup(parsed.Groups[5].Value)
            };

            return new TimeSpan(values.d, values.h, values.m, values.s, values.ms);
        }

        private static string SuffixNumber(string prefix, int number) => 
            number <= 0 ? string.Empty : $"{number}{prefix}";

        public void WriteYaml(IEmitter emitter, object value, Type type)
        {
            var timespan = (TimeSpan)value!;
            var formatted = SuffixNumber("d", timespan.Days) +
                SuffixNumber("h", timespan.Hours) +
                SuffixNumber("m", timespan.Minutes) +
                SuffixNumber("s", timespan.Seconds);

            emitter.Emit(new Scalar(null, null, formatted, ScalarStyle.Any, true, false));
        }
    }
}

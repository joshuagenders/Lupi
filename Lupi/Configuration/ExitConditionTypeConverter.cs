using System;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Lupi.Configuration
{
    public class ExitConditionTypeConverter : IYamlTypeConverter
    {
        private static Regex TimeStringRegex = 
            new Regex(@"^(passed|failed) if (.+) ([<>=]{1,2}) (\d+\.{0,1}\d*) for (\d+) (seconds|periods|minutes)$",
                RegexOptions.Compiled);

        public bool Accepts(Type type)
        {
            return type.Equals(typeof(ExitCondition));
        }

        public object ReadYaml(IParser parser, Type type)
        {
            var value = parser.Consume<Scalar>().Value;
            if (string.IsNullOrWhiteSpace(value))
            {
                return new ExitCondition();
            }
            var parsed = TimeStringRegex.Match(value);
            if (!parsed.Success)
            {
                return new ExitCondition();
            }

            var ParseDouble = new Func<string, double>(s => 
                string.IsNullOrWhiteSpace(s) ? 0 : Convert.ToDouble(s));
          
            var values = new
            {
                passedFailed = parsed.Groups[1].Value,
                property = parsed.Groups[2].Value,
                op = parsed.Groups[3].Value,
                value = ParseDouble(parsed.Groups[4].Value),
                period = ParseDouble(parsed.Groups[5].Value),
                periodType = parsed.Groups[6].Value,                
            };

            var exitCondition = new ExitCondition
            {
                Operator = values.op,
                Value = values.value,
                PassedFailed = values.passedFailed,
                Property = values.property
            };
            switch (values.periodType)
            {
                case "seconds":
                    exitCondition.Duration = TimeSpan.FromSeconds(values.period);
                    break;
                case "minutes":
                    exitCondition.Duration = TimeSpan.FromMinutes(values.period);
                    break;
                case "periods":
                    exitCondition.Periods = Convert.ToInt32(values.period);
                    break;
            }
            return exitCondition;
        }

        public void WriteYaml(IEmitter emitter, object value, Type type)
        {
            var exitCondition = (ExitCondition)value!;
            var formatted = $"{exitCondition.PassedFailed} if {exitCondition.Property} {exitCondition.Operator} {exitCondition.Value} for ";
            if (exitCondition.Periods > 0)
            {
                formatted += $"{exitCondition.Periods} periods";
            }
            else if (exitCondition.Duration.TotalSeconds >= 60)
            {
                formatted += $"{exitCondition.Duration.TotalMinutes} minutes";
            }
            else if (exitCondition.Duration.TotalMilliseconds > 0)
            {
                formatted += $"{exitCondition.Duration.TotalSeconds} seconds";
            }

            emitter.Emit(new Scalar(null, null, formatted, ScalarStyle.Any, true, false));
        }
    }
}

﻿using System;
using System.Text.RegularExpressions;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Lupi.Configuration
{
    public class ExitConditionTypeConverter : IYamlTypeConverter
    {
        private static Regex TimeStringRegex = 
            new Regex(@"^(\w)* [<>=]{1,2} (\d+\.{0,1}\d*) for (\d+) [(seconds)|(periods)|(minutes)]+$", 
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

            var ParseGroup = new Func<string, double>(s => 
                string.IsNullOrWhiteSpace(s) ? 0 : double.Parse(s));

            var values = new
            {
                property = ParseGroup(parsed.Groups[1].Value),
                op = parsed.Groups[2].Value,
                value = ParseGroup(parsed.Groups[3].Value),
                period = ParseGroup(parsed.Groups[4].Value),
                periodType = parsed.Groups[5].Value
            };

            var exitCondition = new ExitCondition
            {
                Operator = values.op,
                Value = values.value
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
            var formatted = $"{exitCondition.Property} {exitCondition.Operator} {exitCondition.Value} for ";
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

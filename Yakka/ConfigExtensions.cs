using System;
using System.Collections.Generic;
using System.Linq;

namespace Yakka
{
    public static class ConfigExtensions
    {
        public static TimeSpan TestDuration (this Config config)
        {
            if (config.Throughput.Phases.Any())
            {
                return config.Throughput.Phases
                    .Select(p => p.Duration)
                    .Aggregate((acc, a) => acc.Add(a));
            }
            else
            {
                return config.Throughput.RampUp.Add(
                       config.Throughput.HoldFor.Add(
                       config.Throughput.RampDown));
            }
        }

        public static List<ThroughputPhase> BuildStandardThroughputPhases(this Config config)
        {
            var phases = new List<ThroughputPhase>();
            if (config.Throughput.RampUp.TotalMilliseconds > 0)
            {
                phases.Add(new ThroughputPhase
                {
                    Duration = config.Throughput.RampUp,
                    FromRps = 0,
                    ToRps = config.Throughput.Tps
                });
            }
            if (config.Throughput.HoldFor.TotalMilliseconds > 0)
            {
                phases.Add(new ThroughputPhase
                {
                    Duration = config.Throughput.HoldFor,
                    Rps = config.Throughput.Tps
                });
            }
            if (config.Throughput.RampDown.TotalMilliseconds > 0)
            {
                phases.Add(new ThroughputPhase
                {
                    Duration = config.Throughput.RampDown,
                    FromRps = config.Throughput.Tps,
                    ToRps = 0
                });
            }
            return phases;
        }
    }
}
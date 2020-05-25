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

        public static List<Phase> BuildStandardThroughputPhases(this Config config)
        {
            var phases = new List<Phase>();
            if (config.Throughput.RampUp.TotalMilliseconds > 0)
            {
                phases.Add(new Phase
                {
                    Duration = config.Throughput.RampUp,
                    FromTps = 0,
                    ToTps = config.Throughput.Tps
                });
            }
            if (config.Throughput.HoldFor.TotalMilliseconds > 0)
            {
                phases.Add(new Phase
                {
                    Duration = config.Throughput.HoldFor,
                    Tps = config.Throughput.Tps
                });
            }
            if (config.Throughput.RampDown.TotalMilliseconds > 0)
            {
                phases.Add(new Phase
                {
                    Duration = config.Throughput.RampDown,
                    FromTps = config.Throughput.Tps,
                    ToTps = 0
                });
            }
            return phases;
        }

        public static double GetPhaseTotalThroughput(this Phase phase) =>
           phase.Tps > 0
               ? phase.Duration.TotalMilliseconds * phase.Tps / 1000
               : phase.Duration.TotalMilliseconds * (phase.ToTps - phase.FromTps) / 2 / 1000 
                    + phase.Duration.TotalMilliseconds * phase.FromTps / 1000;

        public static double GetPhaseThroughput(this Phase phase, int millisecondsEllapsed)
        {
            if (phase.Tps > 0)
            {
                return millisecondsEllapsed * phase.Tps / 1000;
            }
            else if (phase.FromTps == phase.ToTps)
            {
                return millisecondsEllapsed * phase.ToTps / 1000;
            }
            else if (phase.FromTps < phase.ToTps)
            {
                return (millisecondsEllapsed * (phase.ToTps - phase.FromTps)) / 2 + millisecondsEllapsed * phase.FromTps / 1000;
            }
            else
            {
                var phaseTotal = phase.Duration.TotalMilliseconds * (phase.FromTps - phase.ToTps) / 2 
                    + phase.Duration.TotalMilliseconds * phase.ToTps;
                var millisecondsRemaining = phase.Duration.TotalMilliseconds - millisecondsEllapsed;
                var remainingTotal = millisecondsRemaining * (phase.FromTps - phase.ToTps) / 2 + millisecondsRemaining * phase.ToTps;
                return (phaseTotal - remainingTotal) / 1000;
            }
        }

        public static int TotalAllowedRequestsToNow(this List<Phase> phases, int millisecondsEllapsed)
        {
            var totalTime = 0;
            var totalRequests = 0d;
            Phase currentPhase = null;
            foreach (var phase in phases)
            {
                if (millisecondsEllapsed > totalTime + phase.Duration.TotalMilliseconds)
                {
                    break;
                }
                totalTime += Convert.ToInt32(phase.Duration.TotalMilliseconds);
                totalRequests += phase.GetPhaseTotalThroughput();
                currentPhase = phase;
            }
            return Convert.ToInt32(totalRequests + currentPhase?.GetPhaseThroughput(millisecondsEllapsed - totalTime) ?? 0);
        }
    }
}
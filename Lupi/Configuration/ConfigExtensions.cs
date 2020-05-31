using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lupi.Configuration
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
            else if (config.Concurrency.Phases.Any())
            {
                return config.Concurrency.Phases
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

        public static List<ConcurrencyPhase> BuildStandardConcurrencyPhases(this Config config)
        {
            var phases = new List<ConcurrencyPhase>();
            if (config.Concurrency.OpenWorkload)
            {
                return phases;
            }
            if (config.Concurrency.RampUp.TotalMilliseconds > 0)
            {
                phases.Add(new ConcurrencyPhase
                {
                    Duration = config.Concurrency.RampUp,
                    FromThreads = 0,
                    ToThreads = config.Concurrency.Threads
                });
            }
            if (config.Throughput.HoldFor.TotalMilliseconds > 0)
            {
                phases.Add(new ConcurrencyPhase
                {
                    Duration = config.Throughput.HoldFor,
                    Threads = config.Concurrency.Threads
                });
            }
            if (config.Concurrency.RampDown.TotalMilliseconds > 0)
            {
                phases.Add(new ConcurrencyPhase
                {
                    Duration = config.Concurrency.RampDown,
                    FromThreads = config.Concurrency.Threads,
                    ToThreads = 0
                });
            }
            return phases;
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
               : phase.Duration.TotalMilliseconds * Math.Abs(phase.ToTps - phase.FromTps) / 2 / 1000 
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
                return (millisecondsEllapsed * (phase.ToTps - phase.FromTps)) / 2 / 1000 + millisecondsEllapsed * phase.FromTps / 1000;
            }
            else
            {//todo refactor this
                var phaseTotal = phase.Duration.TotalMilliseconds * (phase.FromTps - phase.ToTps) / 2 
                    + phase.Duration.TotalMilliseconds * phase.ToTps;
                var millisecondsRemaining = phase.Duration.TotalMilliseconds - millisecondsEllapsed;
                var remainingTotal = millisecondsRemaining * (phase.FromTps - phase.ToTps) / 2 + millisecondsRemaining * phase.ToTps;
                return (phaseTotal - remainingTotal) / 1000;
            }
        }

        public static int GetPhaseThreadCount(this ConcurrencyPhase phase, int millisecondsEllapsed)
        {
            var percentageThroughPhase = millisecondsEllapsed / phase.Duration.TotalMilliseconds;
            if (percentageThroughPhase > 1)
            {
                DebugHelper.Write("Get phase thread count past the phase end");
                return 0;
            }

            if (phase.Threads > 0 || phase.FromThreads == phase.ToThreads)
            {
                return phase.Threads;
            }
            else if (phase.FromThreads < phase.ToThreads)
            {//todo refactor this

                var diff = phase.ToThreads - phase.FromThreads;
                var extraThreads = diff * percentageThroughPhase;
                return Convert.ToInt32(extraThreads) + phase.FromThreads;
            }
            else
            {
                var diff = phase.FromThreads - phase.ToThreads;
                var lessThreads = diff * percentageThroughPhase;
                return phase.FromThreads - Convert.ToInt32(lessThreads);
            }
        }

        public static int TotalAllowedRequestsToNow(this IEnumerable<Phase> phases, int millisecondsEllapsed)
        {
            var currentPhase = phases.Select(
                (p, i) => new
                {
                    Phase = p,
                    PhaseStart = phases.Where((x, n) => i < n)
                                       .Sum(x => x.Duration.TotalMilliseconds),
                    PhaseStartTps = phases.Where((x, n) => i < n)
                                       .Sum(x => x.GetPhaseTotalThroughput()),
                })
                .Where(s => s.PhaseStart < millisecondsEllapsed && s.Phase.Duration.TotalMilliseconds + s.PhaseStart > millisecondsEllapsed)
                .FirstOrDefault();
            if (currentPhase == null)
            {
                DebugHelper.Write($"Could not determine current throughput phase. milliseconds ellaped {millisecondsEllapsed}");
                return 0;
            }
            var millisecondsThroughPhase = Convert.ToInt32(millisecondsEllapsed - currentPhase.PhaseStart);
            var allowedRequests = Convert.ToInt32(currentPhase.PhaseStartTps + currentPhase.Phase.GetPhaseThroughput(millisecondsThroughPhase));

            return allowedRequests;
        }

        public static int CurrentDesiredThreadCount(this IEnumerable<ConcurrencyPhase> phases, int millisecondsEllapsed)
        {
            var currentPhase = phases.Select(
                (p, i) => new
                {
                    Phase = p,
                    PhaseStart = phases.Where((x, n) => i < n)
                                       .Sum(x => x.Duration.TotalMilliseconds),
                })
                .Where(s => s.PhaseStart <= millisecondsEllapsed && s.Phase.Duration.TotalMilliseconds + s.PhaseStart > millisecondsEllapsed)
                .FirstOrDefault();
            if (currentPhase == null)
            {
                DebugHelper.Write($"Could not determine current concurrency phase. milliseconds ellapsed {millisecondsEllapsed}");
                DebugHelper.Write(JsonConvert.SerializeObject(phases));
                return 0;
            }
            var millisecondsThroughPhase = Convert.ToInt32(millisecondsEllapsed - currentPhase.PhaseStart);
            
            return currentPhase.Phase.GetPhaseThreadCount(millisecondsThroughPhase);
        }
    }
}
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
                var throughputDuration = config.Throughput.RampUp.Add(
                       config.Throughput.HoldFor.Add(
                       config.Throughput.RampDown));
                var concurrencyDuration = config.Concurrency.RampUp.Add(
                       config.Concurrency.HoldFor.Add(
                       config.Concurrency.RampDown));
                return new TimeSpan[] { throughputDuration, concurrencyDuration }.Max();
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

            if (config.Concurrency.HoldFor.TotalMilliseconds > 0)
            {
                phases.Add(new ConcurrencyPhase
                {
                    Duration = config.Concurrency.HoldFor,
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

        public static double GetTokensForPeriod(this IEnumerable<Phase> phases, DateTime startTime, DateTime lastTime, DateTime now)
        {
            var lastMsThroughTest = lastTime.Subtract(startTime).TotalMilliseconds;
            var msThroughTest = now.Subtract(startTime).TotalMilliseconds;

            var x = phases.Select(
               (p, i) => new
               {
                   Phase = p,
                   PhaseStart = phases.Where((x, n) => n < i)
                                      .Sum(x => x.Duration.TotalMilliseconds + 1),
                   PhaseEnd = phases.Where((x, n) => n < i)
                                      .Sum(x => x.Duration.TotalMilliseconds + 1) + p.Duration.TotalMilliseconds,
               });
            var y2 = x
               .Where(p => p.PhaseStart <= msThroughTest
                        && p.PhaseEnd >= msThroughTest);
            var y = y2.Select(p => new
            {
                p.Phase,
                x1 = Math.Max(p.PhaseStart, lastMsThroughTest) - p.PhaseStart,
                x2 = Math.Min(p.PhaseEnd, msThroughTest) - p.PhaseStart
            });
            var z = y.Select(p =>
            {
                var length = Math.Abs(p.x2 - p.x1);
                if (p.Phase.Tps > 0)
                {
                    return p.Phase.Tps * length / 1000;
                }
                else
                {
                    var gradient =
                        Math.Abs(p.Phase.ToTps - p.Phase.FromTps)
                        / p.Phase.Duration.TotalMilliseconds;
                    var lastY = p.x1 * gradient;
                    var y = p.x2 * gradient;
                    var squareArea = length * Math.Min(p.Phase.ToTps, p.Phase.FromTps);
                    var triangleArea = length * Math.Abs(y - lastY) / 2;
                    return (triangleArea + squareArea) / 1000;
                }
            })
            .Sum();
            return z;
        }

        public static int CurrentDesiredThreadCount(this IEnumerable<ConcurrencyPhase> phases, DateTime startTime, DateTime now)
        {
            var phaseStartTimes = phases.Select(
                (p, i) => new
                {
                    Phase = p,
                    PhaseStart = phases.Where((x, n) => n < i)
                                       .Sum(x => x.Duration.TotalMilliseconds)
                });
            var currentPhase = phaseStartTimes
                .Where(p => startTime.AddMilliseconds(p.PhaseStart) <= now)
                .Where(p => startTime.AddMilliseconds(p.PhaseStart).Add(p.Phase.Duration) >= now)
                .FirstOrDefault();
            if (currentPhase == null)
            {
                DebugHelper.Write($"Could not determine current concurrency phase.");
                DebugHelper.Write(JsonConvert.SerializeObject(phases));
                return 0;
            }
            if (currentPhase.Phase.Threads > 0)
            {
                return currentPhase.Phase.Threads;
            }
            else
            {
                var gradient = 
                    (currentPhase.Phase.ToThreads - currentPhase.Phase.FromThreads)
                    / currentPhase.Phase.Duration.TotalMilliseconds;
                var phaseStart = startTime.AddMilliseconds(currentPhase.PhaseStart);
                var x = (now.Subtract(phaseStart).TotalMilliseconds);
                var result = Convert.ToInt32(currentPhase.Phase.FromThreads + x * gradient);
                DebugHelper.Write($"THREADS: {JsonConvert.SerializeObject(new {gradient, x, phaseStart, result, currentPhase})}");
                return result;
            }
        }
    }
}

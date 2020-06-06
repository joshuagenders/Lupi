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
            else if (config.Throughput.Phases.Any())
            {
                phases.Add(new ConcurrencyPhase
                {
                    Duration = config.Throughput
                        .Phases
                        .Aggregate(TimeSpan.Zero, (acc, val) => acc.Add(val.Duration)),
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
            var phaseStartTimes = phases.Select(
                (p, i) => new
                {
                    Phase = p,
                    PhaseStart = phases.Where((x, n) => i < n)
                                       .Sum(x => x.Duration.TotalMilliseconds)
                });
            var lastPhase = phaseStartTimes
                .Where(p => startTime.AddMilliseconds(p.PhaseStart) <= lastTime)
                .Where(p => startTime.AddMilliseconds(p.PhaseStart).Add(p.Phase.Duration) >= lastTime)
                .FirstOrDefault();
            var currentPhase = phaseStartTimes
                .Where(p => startTime.AddMilliseconds(p.PhaseStart) <= now)
                .Where(p => startTime.AddMilliseconds(p.PhaseStart).Add(p.Phase.Duration) >= now)
                .FirstOrDefault();

            if (currentPhase == null)
            {
                DebugHelper.Write($"Could not determine current throughput phase.");
                return 0;
            }
            
            //if constant
            var phaseStart = startTime.AddMilliseconds(currentPhase.PhaseStart);
            var lastX = lastTime.Subtract(phaseStart).TotalMilliseconds;
            var result = 0d;
            if (lastX < 0)
            {
                //get last phase remaining amount
                var lastPhaseEnd = startTime.AddMilliseconds(lastPhase.PhaseStart)
                                            .Add(lastPhase.Phase.Duration)
                                            .Subtract(TimeSpan.FromMilliseconds(1));

                result += phases.GetTokensForPeriod(startTime, lastTime, lastPhaseEnd);
                lastX = 0;
            }
            var x = now.Subtract(phaseStart).TotalMilliseconds;
            if (currentPhase.Phase.Tps > 0)
            {
                return currentPhase.Phase.Tps * (x - lastX) / 1000;
            }

            //if gradient
            var gradient =
                Math.Abs(currentPhase.Phase.ToTps - currentPhase.Phase.FromTps) 
                / currentPhase.Phase.Duration.TotalMilliseconds;
               
            var lastY = lastX * gradient;
            var y = x * gradient;
            var squareArea = Math.Abs(x - lastX) * lastY;
            var triangleArea = Math.Abs(y - lastY) * Math.Abs(x - lastX) / 2;
            result += (triangleArea + squareArea) / 1000;
            return result;
        }

        public static int CurrentDesiredThreadCount(this IEnumerable<ConcurrencyPhase> phases, DateTime startTime, DateTime now)
        {
            var phaseStartTimes = phases.Select(
                (p, i) => new
                {
                    Phase = p,
                    PhaseStart = phases.Where((x, n) => i < n)
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
                return Convert.ToInt32(currentPhase.Phase.FromThreads + x * gradient);
            }
        }
    }
}
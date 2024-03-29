﻿namespace Lupi.Configuration
{
    public static class ConfigExtensions
    {
        public static TimeSpan TestDuration(this Config config)
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

            var phaseTimes = phases.Select(
               (p, i) => new
               {
                   Phase = p,
                   PhaseStart = phases.Where((x, n) => n < i)
                                      .Sum(x => x.Duration.TotalMilliseconds),
                   PhaseEnd = phases.Where((x, n) => n < i)
                                      .Sum(x => x.Duration.TotalMilliseconds) + p.Duration.TotalMilliseconds,
               });
            var currentPhases = phaseTimes
               .Where(p => p.PhaseStart <= msThroughTest && msThroughTest < p.PhaseEnd
                        || p.PhaseStart <= lastMsThroughTest && lastMsThroughTest < p.PhaseEnd);
            var currentPhasesCoordinates = currentPhases.Select(p => new
            {
                p.Phase,
                lastX = Math.Max(p.PhaseStart, lastMsThroughTest) - p.PhaseStart,
                x = Math.Min(p.PhaseEnd, msThroughTest) - p.PhaseStart
            });

            var result = currentPhasesCoordinates.Select(p =>
            {
                var length = Math.Abs(p.x - p.lastX);
                if (p.Phase.Tps > 0)
                {
                    return p.Phase.Tps * length / 1000;
                }
                else
                {
                    var gradient =
                        Math.Abs(p.Phase.ToTps - p.Phase.FromTps)
                        / p.Phase.Duration.TotalMilliseconds;
                    var lastY = p.lastX * gradient;
                    var y = p.x * gradient;
                    double squareArea = 0;
                    if (p.Phase.FromTps < p.Phase.ToTps)
                    {
                        squareArea =  length * lastY;
                    }
                    else
                    {
                        squareArea = length * (p.Phase.FromTps - y);
                    }
                    var triangleArea = length * Math.Abs(y - lastY) / 2;
                    var result = (triangleArea + squareArea) / 1000;
                    return result;
                }
            })
            .Sum();
            return result;
        }

        public static int CurrentDesiredThreadCount(
            this IEnumerable<ConcurrencyPhase> phases, 
            DateTime startTime, 
            DateTime now)
        {
            var phaseStartTimes = phases.Select(
                (p, i) => new
                {
                    Phase = p,
                    PhaseStart = startTime.AddMilliseconds(phases.Where((x, n) => n < i)
                                          .Sum(x => x.Duration.TotalMilliseconds))
                });
            var currentPhase = phaseStartTimes
                .Where(p => p.PhaseStart <= now
                         && now < p.PhaseStart.Add(p.Phase.Duration))
                .FirstOrDefault();
            if (currentPhase == null)
            {
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
                var x = (now.Subtract(currentPhase.PhaseStart).TotalMilliseconds);
                return Convert.ToInt32(currentPhase.Phase.FromThreads + x * gradient);
            }
        }

        public static List<string> Validate(this Config config)
        {
            var validator = new ConfigurationValidator();
            var result = validator.Validate(config);
            return result.Errors.Select(r => r.ErrorMessage).ToList();
        }
    }
}

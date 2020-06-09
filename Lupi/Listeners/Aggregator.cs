using Lupi.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Lupi.Listeners
{
    public interface IAggregator
    {
        void Subscribe(IAggregatorListener listener);
        Task Process(CancellationToken ct);
        bool TestCompleted { get; set; }
    }

    public class Aggregator : ITestResultListener, IAggregator
    {
        private readonly List<IAggregatorListener> _listeners;
        private readonly Config _config;
        private readonly ConcurrentBag<TestResult> _results;

        private const int FACTOR = 4;
        private double _expMovingAverage { get; set; }
        private double _max { get; set; }
        private double _min { get; set; } = double.MaxValue;
        private long _counter { get; set; }

        public bool TestCompleted { get; set; } = false;
        public Aggregator(Config config)
        {
            _config = config;
            _listeners = new List<IAggregatorListener>();
            _results = new ConcurrentBag<TestResult>();
        }

        public void Subscribe(IAggregatorListener listener)
        {
            _listeners.Add(listener);
        }

        public async Task Process(CancellationToken ct)
        {
            if (!_listeners.Any())
            {
                return;
            }
            while (!ct.IsCancellationRequested && !TestCompleted || _results.Any())
            {
                var results = new List<TestResult>();
                while (_results.TryTake(out var r) && results.Count < 10000)
                {
                    _counter++;
                    _expMovingAverage =
                        _expMovingAverage + (r.Duration.TotalMilliseconds - _expMovingAverage) /
                        Math.Min(_counter, FACTOR);
                    results.Add(r);
                }
                if (results.Any())
                {
                    var periodLength = results.Max(r => r.FinishedTime).Subtract(results.Min(r => r.FinishedTime));
                    var periodAvg = results.Average(r => r.Duration.TotalMilliseconds);
                    var periodMax = results.Max(r => r.Duration.TotalMilliseconds);
                    var periodMin = results.Min(r => r.Duration.TotalMilliseconds);
                    _min = Math.Min(_min, periodMin);
                    _max = Math.Max(_max, periodMax);
                    var aggregated = new AggregatedResult
                    {
                        Min = _min,
                        Max = _max,
                        MovingAverage = _expMovingAverage,
                        PeriodMin = periodMin,
                        PeriodMax = periodMax,
                        PeriodAverage = periodAvg
                    };
                    await Task.WhenAll(_listeners.Select(l => l.OnResult(aggregated, ct)));
                }
                await Task.Delay(_config.Engine.AggregationInterval, ct);
            }
        }

        public async Task OnResult(TestResult[] results, CancellationToken ct)
        {
            foreach (var result in results)
            {
                _results.Add(result);
            }
            await Task.CompletedTask;
        }
    }
}

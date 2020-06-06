using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lupi.Configuration;
using JustEat.StatsD;

namespace Lupi
{
    public class ThreadControl : IThreadControl
    {
        private readonly Config _config;
        private readonly IPlugin _plugin;
        private readonly ITestResultPublisher _testResultPublisher;
        private readonly StatsDPublisher _stats;

        private readonly List<Task> _tasks;
        private readonly SemaphoreSlim _taskExecution;
        private readonly SemaphoreSlim _taskDecrement;
        private readonly ConcurrentQueue<bool> _taskKill;

        private int _iterationsRemaining;
        
        public ThreadControl(Config config, IPlugin plugin, ITestResultPublisher testResultPublisher)
        {
            _config = config;
            _plugin = plugin;
            _testResultPublisher = testResultPublisher;

            _taskExecution = new SemaphoreSlim(0);
            _taskDecrement = new SemaphoreSlim(1);
            _taskKill = new ConcurrentQueue<bool>();
            _tasks = new List<Task>();
            _iterationsRemaining = config.Throughput.Iterations;

            if (!string.IsNullOrWhiteSpace(_config.Listeners.Statsd.Host))
            {
                _stats = new StatsDPublisher(new StatsDConfiguration
                {
                    Host = _config.Listeners.Statsd.Host,
                    Port = _config.Listeners.Statsd.Port,
                    Prefix = _config.Listeners.Statsd.Prefix
                });
            }
        }

        public async Task Run(DateTime startTime, CancellationToken ct)
        {
            //todo run setup and teardown methods
            var endTime = startTime.Add(_config.TestDuration());
            var lastTime = DateTime.UtcNow;
            var partialTokens = 0d;
            var iterationsRemaining = _config.Throughput.Iterations;
            while (DateTime.UtcNow < endTime && !ct.IsCancellationRequested)
            {
                if (_config.Throughput.Iterations > 0 && iterationsRemaining <= 0)
                {
                    //here?
                    return;
                }
                var now = DateTime.UtcNow;
                // execution tokens
                var tokensToRelease = _config.Throughput.Phases.GetTokensForPeriod(startTime, lastTime, now);
                var wholeTokens = Convert.ToInt32(tokensToRelease);

                partialTokens += tokensToRelease - wholeTokens;
                if (partialTokens >= 1)
                {
                    wholeTokens += Convert.ToInt32(partialTokens);
                    partialTokens -= Math.Truncate(partialTokens);
                }
                lastTime = now;
                if (_config.Throughput.Iterations > 0 && iterationsRemaining - wholeTokens < 0)
                {
                    wholeTokens = iterationsRemaining;
                }
                if (wholeTokens > 0)
                {
                    DebugHelper.Write($"releasing {wholeTokens} tokens");
                    _stats?.Increment(wholeTokens, $"{_config.Listeners.Statsd.Bucket}.releasedtoken");
                    _taskExecution.Release(wholeTokens);
                }

                // threads
                _tasks.RemoveAll(x => x.IsCompleted);
                var threadCount = _tasks.Count;

                DebugHelper.Write($"thread count {threadCount}");
                if (_config.Concurrency.OpenWorkload)
                {
                    DebugHelper.Write("calculate threads for open workload");
                    if (threadCount < _config.Concurrency.MinThreads)
                    {
                        DebugHelper.Write("concurrency less than min");
                        AdjustThreads(startTime, _config.Concurrency.MinThreads - threadCount, ct);
                    }

                    var currentCount = _taskExecution.CurrentCount;
                    if (currentCount > 1)
                    {
                        AdjustThreads(startTime, currentCount - 1, ct);
                    }
                }
                else
                {
                    DebugHelper.Write("calculate threads for closed workload");
                    var desired = _config.Concurrency.Phases.CurrentDesiredThreadCount(startTime, now);
                    var current = _tasks.Count;
                    DebugHelper.Write($"desired threads: {desired}. current threads: {current}");
                    if (desired > current)
                    {
                        DebugHelper.Write("desired greater than current");
                        StartTask(startTime, ct);
                    }
                    else if (desired < current)
                    {
                        DebugHelper.Write("current greater than desired, requesting task kill");
                        _stats?.Increment($"{_config.Listeners.Statsd.Bucket}.taskkillrequested");
                        for (int i = 0; i < current - desired; i++)
                        {
                            _taskKill.Enqueue(true);
                        }
                    }
                }
                DebugHelper.Write($"main loop complete. thread count {_tasks.Count}");
                _stats?.Gauge(_tasks.Count, $"{_config.Listeners.Statsd.Bucket}.threads");

                await Task.Delay(_config.Engine.CheckInterval, ct);
            }

            _stats?.Gauge(0, $"{_config.Listeners.Statsd.Bucket}.threads");
        }

        public bool RequestTaskContinuedExecution()
        {
            if (_config.ThroughputEnabled)
            {
                if (_taskKill.TryDequeue(out var result))
                {
                    return false;
                }
            }
            return true;
        }

        public async Task<bool> RequestTaskExecution(DateTime startTime, CancellationToken ct)
        {
            DebugHelper.Write($"task execution request start");
            if (!RequestTaskContinuedExecution())
            {
                DebugHelper.Write($"found kill token. dying.");
                return false;
            }

            _stats?.Increment($"{_config.Listeners.Statsd.Bucket}.requesttaskexecution");
            int iterations = 0;
            if (_config.Throughput.Iterations > 0)
            {
                try
                {
                    await _taskDecrement.WaitAsync(ct);
                    iterations = Interlocked.Decrement(ref _iterationsRemaining);
                    DebugHelper.Write($"iterations: {_config.Throughput.Iterations}. iterations left {iterations}");
                }
                finally
                {
                    _taskDecrement.Release();
                }
            }
            if (_config.ThroughputEnabled)
            {
                DebugHelper.Write($"task execution enabled, waiting");
                await _taskExecution.WaitAsync(ct);
                DebugHelper.Write($"waiting complete");
            }

            DebugHelper.Write($"executions remaining {iterations}");
            var isCompleted = IsTestComplete(startTime, iterations);
            DebugHelper.Write($"is test completed {isCompleted} iterations {iterations} max iterations {_config.Throughput.Iterations}");
            return isCompleted;
        }

        private void AdjustThreads(DateTime startTime, int amount, CancellationToken ct)
        {
            if (amount > 0)
            {
                for (var i = 0; i < amount; i++)
                {
                    StartTask(startTime, ct);
                }
            }
            else if (amount < 0)
            {
                for (var i = 0; i > amount; i--)
                {
                    _stats?.Increment($"{_config.Listeners.Statsd.Bucket}.taskkillrequested");
                    _taskKill.Enqueue(true);
                }
            }
        }

        private bool StartTask(DateTime startTime, CancellationToken ct)
        {
            _stats?.Increment($"{_config.Listeners.Statsd.Bucket}.starttask");
            if (_config.Concurrency.OpenWorkload 
                && _tasks.Count >= _config.Concurrency.MaxThreads)
            {
                return false;
            }

            _tasks.Add(Task.Run(() => 
                new TestThread(this, _plugin, _testResultPublisher, _stats, _config)
                    .Run(startTime, ct), ct));
            return true;
        }

        private bool IsTestComplete(DateTime startTime, int iterationsRemaining) =>
            DateTime.UtcNow >= startTime.Add(_config.TestDuration()) 
            || (iterationsRemaining < 0 && _config.Throughput.Iterations > 0);
    }

    public interface IThreadControl
    {
        Task Run(DateTime startTime, CancellationToken ct);
        Task<bool> RequestTaskExecution(DateTime startTime, CancellationToken ct);
    }
}

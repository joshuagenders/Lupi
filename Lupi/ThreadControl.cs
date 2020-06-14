using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lupi.Configuration;
using JustEat.StatsD;
using Microsoft.Extensions.Logging;

namespace Lupi
{
    public class ThreadControl : IThreadControl
    {
        private readonly Config _config;
        private readonly IPlugin _plugin;
        private readonly ITestResultPublisher _testResultPublisher;
        private readonly ILogger<IThreadControl> _logger;
        private readonly ILoggerFactory _loggerFactory;

        private readonly StatsDPublisher _stats;

        private readonly List<Task> _tasks;
        private readonly SemaphoreSlim _taskExecution;
        private readonly SemaphoreSlim _taskDecrement;
        private readonly ConcurrentQueue<bool> _taskKill;

        private int _iterationsRemaining;
        
        public ThreadControl(
            Config config, 
            IPlugin plugin, 
            ITestResultPublisher testResultPublisher, 
            ILogger<ThreadControl> logger, 
            ILoggerFactory loggerFactory)
        {
            _config = config;
            _plugin = plugin;
            _testResultPublisher = testResultPublisher;
            _logger = logger;
            _loggerFactory = loggerFactory;

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
            await _plugin.ExecuteSetupMethod();
            try 
            {
                var endTime = startTime.Add(_config.TestDuration());
                var lastTime = DateTime.UtcNow;
                var partialTokens = 0d;
                var iterationsRemaining = _config.Throughput.Iterations;
                while (DateTime.UtcNow < endTime && !ct.IsCancellationRequested)
                {
                    if (_config.Throughput.Iterations > 0 && iterationsRemaining <= 0)
                    {
                        _logger.LogInformation("Iteration count reached");
                        return;
                    }
                    var now = DateTime.UtcNow;
                    // execution tokens
                    var tokensToRelease = _config.Throughput.Phases.GetTokensForPeriod(startTime, lastTime, now);
                    lastTime = now;

                    var wholeTokens = Convert.ToInt32(tokensToRelease);
                    partialTokens += tokensToRelease - wholeTokens;
                    if (partialTokens >= 1)
                    {
                        wholeTokens += Convert.ToInt32(partialTokens);
                        partialTokens -= Math.Truncate(partialTokens);
                    }
                    
                    if (_config.Throughput.Iterations > 0 && iterationsRemaining - wholeTokens < 0)
                    {
                        wholeTokens = iterationsRemaining;
                    }

                    _logger.LogInformation("Calculated tokens. {wholeTokens} tokens. {partialTokens} partialTokens", wholeTokens, partialTokens);
                    if (wholeTokens > 0)
                    {
                        _logger.LogInformation("Releasing {wholeTokens} tokens", wholeTokens);
                        _stats?.Increment(wholeTokens, $"{_config.Listeners.Statsd.Bucket}.releasedtoken");
                        _taskExecution.Release(wholeTokens);
                    }

                    // threads
                    _tasks.RemoveAll(x => x.IsCompleted);
                    var threadCount = _tasks.Count;
                    _logger.LogInformation("Thread count {threadCount}", threadCount);
                    if (_config.Concurrency.OpenWorkload)
                    {
                        _logger.LogInformation("Calculate threads for open workload");
                        if (threadCount < _config.Concurrency.MinThreads)
                        {
                            SetThreadLevel(startTime, _config.Concurrency.MinThreads - threadCount, ct);
                        }

                        var currentCount = _taskExecution.CurrentCount;
                        if (currentCount > 1)
                        {
                            var amount = Math.Max(
                                _config.Concurrency.MinThreads,
                                Math.Min(_config.Concurrency.MaxThreads - threadCount, threadCount + currentCount));
                            SetThreadLevel(startTime, amount, ct);
                        }
                    }
                    else
                    {
                        _logger.LogInformation("Calculate threads for closed workload");
                        var desired = _config.Concurrency.Phases.CurrentDesiredThreadCount(startTime, now);
                        _logger.LogInformation("Desired threads: {desired}. Current threads: {threadCount}", desired, threadCount);
                        SetThreadLevel(startTime, desired, ct);
                    }
                    _logger.LogInformation("Main loop complete. thread count {threadCount}", threadCount);
                    _stats?.Gauge(_tasks.Count, $"{_config.Listeners.Statsd.Bucket}.threads");

                    await Task.Delay(_config.Engine.CheckInterval, ct);
                }
            }
            finally
            {
                _stats?.Gauge(0, $"{_config.Listeners.Statsd.Bucket}.threads");
                await _plugin.ExecuteTeardownMethod();
            }
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
            _logger.LogInformation($"Task execution request start");
            _stats?.Increment($"{_config.Listeners.Statsd.Bucket}.requesttaskexecutionstart");

            if (!RequestTaskContinuedExecution())
            {
                _logger.LogInformation($"Found kill token. dying.");
                _stats?.Increment($"{_config.Listeners.Statsd.Bucket}.requesttaskexecutionend");
                _stats?.Increment($"{_config.Listeners.Statsd.Bucket}.taskkill");
                return false;
            }
            if (_config.ThroughputEnabled)
            {
                _logger.LogInformation($"Task execution enabled, waiting");
                await _taskExecution.WaitAsync(ct);
                _logger.LogInformation($"Waiting complete");
            }

            int iterations = 0;
            if (_config.Throughput.Iterations > 0)
            {
                try
                {
                    await _taskDecrement.WaitAsync(ct);
                    iterations = Interlocked.Decrement(ref _iterationsRemaining);
                    _logger.LogInformation("Iterations: {iterations}. Iterations left: {iterationsRemaining}", _config.Throughput.Iterations, iterations);
                }
                finally
                {
                    _taskDecrement.Release();
                }
            }

            var isCompleted = IsTestComplete(startTime, iterations);
            _logger.LogInformation("Is test completed: {isCompleted}. Iterations: {iterations}. Max iterations: {maxIterations}", isCompleted, iterations, _config.Throughput.Iterations);
            _stats?.Increment($"{_config.Listeners.Statsd.Bucket}.requesttaskexecutionend");
            return !isCompleted;
        }

        private void SetThreadLevel(DateTime startTime, int threads, CancellationToken ct)
        {
            var taskCount = _tasks.Count;
            var difference = threads - taskCount;
            if (difference < 0)
            {
                var tasksToKill = Math.Abs(difference) - _taskKill.Count;
                if (tasksToKill > 0)
                {
                    _stats?.Increment(tasksToKill, $"{_config.Listeners.Statsd.Bucket}.taskkillrequested");
                    for (var i = 0; i < tasksToKill; i++)
                    {
                        _taskKill.Enqueue(true);
                    }
                }
            }
            else if (difference > 0)
            {
                _tasks.AddRange(
                    Enumerable.Range(0, difference).Select(_ => 
                        Task.Run(() => 
                            new TestThread(this, _plugin, _testResultPublisher, _stats, _config, _loggerFactory.CreateLogger<TestThread>())
                                .Run(startTime, ct), ct)));
            }
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

using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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

        private async Task<bool> RequestTaskExecution(DateTime startTime, CancellationToken ct)
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

            DebugHelper.Write($"executions {iterations}");
            var isCompleted = IsTestComplete(startTime, iterations);
            DebugHelper.Write($"is test completed {isCompleted} {iterations} {_config.Throughput.Iterations}");
            return isCompleted;
        }

        public async Task ReleaseTokens(DateTime startTime, CancellationToken ct)
        {
            //also release threads in a loop here - only need 1 loop checking things
            // just pass in the start time, last time and current time to get back tokens to release
            if (!_config.ThroughputEnabled)
            {
                return;
            }
            var tokensReleased = 0;
            var endTime = startTime.Add(_config.TestDuration());
            var lastTime = DateTime.UtcNow;
            var partialTokens = 0d;
            while (!IsTestComplete(endTime, tokensReleased) && !ct.IsCancellationRequested)
            {
                var now = DateTime.UtcNow;
                var tokensToRelease = _config.Throughput.Phases.GetTokensForPeriod(startTime, lastTime, now);
                var wholeTokens = Convert.ToInt32(tokensToRelease);

                partialTokens += tokensToRelease - wholeTokens;
                if (partialTokens >= 1) {
                    wholeTokens += Convert.ToInt32(partialTokens);
                    partialTokens -= Math.Truncate(partialTokens);
                }
                lastTime = now;
                if (wholeTokens > 0)
                {
                    DebugHelper.Write($"releasing {wholeTokens} tokens");
                    _stats?.Increment(wholeTokens, $"{_config.Listeners.Statsd.Bucket}.releasedtoken");
                    _taskExecution.Release(wholeTokens);
                }
                await Task.Delay(_config.Engine.TokenGenerationInterval, ct);
            }
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

        public async Task AllocateThreads(DateTime startTime, CancellationToken ct)
        {
            var endTime = startTime.Add(_config.TestDuration());
            var now = DateTime.UtcNow;
            while (DateTime.UtcNow < endTime && !ct.IsCancellationRequested)
            {
                //remove completed tasks 
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
                    else if (desired > current)
                    {
                        DebugHelper.Write("current greater than desired, requesting task kill");
                        _stats?.Increment($"{_config.Listeners.Statsd.Bucket}.taskkillrequested");
                        _taskKill.Enqueue(true);
                    }
                }
                DebugHelper.Write($"thread allocation loop complete. thread count {_tasks.Count}");
                _stats?.Gauge(_tasks.Count, $"{_config.Listeners.Statsd.Bucket}.threads");
                await Task.Delay(_config.Engine.ThreadAllocationInterval);
            }
        }

        private bool StartTask(DateTime startTime, CancellationToken ct)
        {
            _stats?.Increment($"{_config.Listeners.Statsd.Bucket}.starttask");
            var atMax = _config.Concurrency.OpenWorkload
                ? _tasks.Count >= _config.Concurrency.MaxThreads
                : _tasks.Count >= _config.Concurrency.Threads;

            if (!atMax)
            {
                _tasks.Add(Task.Run(() => RunTestLoop(startTime, ct), ct));
                return true;
            }
            return false;
        }

        private async Task RunTestLoop(DateTime startTime, CancellationToken ct)
        {
            _stats?.Increment($"{_config.Listeners.Statsd.Bucket}.taskstart");
            var threadName = $"worker_{Guid.NewGuid().ToString("N")}";
            bool shouldExit = false;
            var watch = new Stopwatch();
            while (!ct.IsCancellationRequested && !shouldExit)
            {
                DebugHelper.Write($"request task execution {threadName}");
                if (_config.Concurrency.OpenWorkload)
                {
                    var taskExecutionRequest = RequestTaskExecution(startTime, ct);
                    var killDelay = Task.Delay(_config.Concurrency.ThreadIdleKillTime);
                    var result = await Task.WhenAny(taskExecutionRequest, killDelay);
                    if (result == killDelay)
                    {
                        DebugHelper.Write($"thread {threadName} got tired of waiting. waited {watch.ElapsedMilliseconds}ms then executed. dying.");
                        break;
                    }
                    else
                    {
                        shouldExit = await taskExecutionRequest;
                        DebugHelper.Write($"task execution request returned {threadName}");
                    }
                }
                else
                {
                    shouldExit = await RequestTaskExecution(startTime, ct);
                    DebugHelper.Write($"task execution request returned {threadName}");
                }

                if (!ct.IsCancellationRequested && !shouldExit)
                {
                    DebugHelper.Write($"test not complete - run method {threadName}");
                    object result;
                    try
                    {
                        watch.Restart();
                        result = _plugin.ExecuteTestMethod();
                        watch.Stop();
                        //todo handle tuple result?
                        var duration = result is TimeSpan ? (TimeSpan)result : watch.Elapsed;

                        _testResultPublisher.Publish(
                            new TestResult
                            {
                                Duration = duration,
                                Passed = true,
                                Result = result.GetType().IsValueType 
                                    ? result.ToString() 
                                    : JsonConvert.SerializeObject(result)
                            });
                    }
                    catch (Exception ex)
                    {
                        watch.Stop();
                        _testResultPublisher.Publish(
                            new TestResult
                            {
                                Duration = watch.Elapsed,
                                Passed = false,
                                Result = JsonConvert.SerializeObject(ex)
                            });
                    }
                    
                    DebugHelper.Write($"method invoke complete {threadName}");
                }
                else
                {
                    DebugHelper.Write($"thread complete {threadName}");
                    break;
                }
                await Task.Delay(_config.Throughput.ThinkTime);
            }
            _stats?.Increment($"{_config.Listeners.Statsd.Bucket}.taskcomplete");
        }

        private bool IsTestComplete(DateTime startTime, int iterationsRemaining) =>
            DateTime.UtcNow >= startTime.Add(_config.TestDuration()) 
            || (iterationsRemaining < 0 && _config.Throughput.Iterations > 0);
    }

    public interface IThreadControl
    {
        Task ReleaseTokens(DateTime startTime, CancellationToken ct);
        Task AllocateThreads(DateTime startTime, CancellationToken ct);
    }
}

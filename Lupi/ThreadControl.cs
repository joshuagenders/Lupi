using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Lupi.Configuration;

namespace Lupi
{
    public class ThreadControl : IThreadControl
    {
        private readonly Config _config;
        private readonly IPlugin _plugin;
        private readonly ITestResultPublisher _testResultPublisher;
        private readonly List<Task> _tasks;
        private readonly SemaphoreSlim _taskExecution;
        private readonly ConcurrentQueue<bool> _taskKill;
        private readonly SemaphoreSlim _taskIncrement;

        private int _executionRequestCount;

        private int _tokensToNow { get; set; }

        public ThreadControl(Config config, IPlugin plugin, ITestResultPublisher testResultPublisher)
        {
            _config = config;
            _plugin = plugin;
            _testResultPublisher = testResultPublisher;

            _taskExecution = new SemaphoreSlim(0);
            _taskIncrement = new SemaphoreSlim(1);
            _taskKill = new ConcurrentQueue<bool>();
            _tasks = new List<Task>();
            _executionRequestCount = 0;
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
            int iterations; //todo use long throughout
            try
            {
                await _taskIncrement.WaitAsync(ct);
                iterations = Interlocked.Increment(ref _executionRequestCount);
            }
            finally
            {
                _taskIncrement.Release();
            }
            if (_config.ThroughputEnabled)
            {
                DebugHelper.Write($"task execution enabled, waiting");
                await _taskExecution.WaitAsync(ct);
                DebugHelper.Write($"waiting complete");
            }

            if (!RequestTaskContinuedExecution())
            {
                DebugHelper.Write($"found kill token. dying.");
                return false;
            }

            DebugHelper.Write($"executions {iterations}");
            var isCompleted = IsTestComplete(startTime, iterations);
            DebugHelper.Write($"is test completed {isCompleted} {iterations} {_config.Throughput.Iterations}");
            return isCompleted;
        }

        public async Task ReleaseTokens(DateTime startTime, CancellationToken ct)
        {
            if (!_config.ThroughputEnabled)
            {
                return;
            }
            var tokensReleased = 0;
            var endTime = startTime.Add(_config.TestDuration());

            while (!IsTestComplete(endTime, tokensReleased) && !ct.IsCancellationRequested)
            {
                var millisecondsEllapsed = Convert.ToInt32(DateTime.UtcNow.Subtract(startTime).TotalMilliseconds);
                _tokensToNow = _config.Throughput.Phases.TotalAllowedRequestsToNow(millisecondsEllapsed);

                if (_tokensToNow > tokensReleased)
                {
                    var tokensToRelease = Convert.ToInt32(_tokensToNow - tokensReleased);
                    if (_config.Throughput.Iterations > 0)
                    {
                        if (int.MaxValue - tokensToRelease < tokensReleased)
                        {
                            tokensToRelease = int.MaxValue - tokensReleased;
                        }
                        if (tokensToRelease + tokensReleased > _config.Throughput.Iterations)
                        {
                            tokensToRelease = Convert.ToInt32(_config.Throughput.Iterations) - tokensReleased;
                        }
                    }
                    if (tokensToRelease > 0)
                    {
                        DebugHelper.Write($"releasing {tokensToRelease}");
                        tokensReleased += tokensToRelease;
                        DebugHelper.Write($"total tokens released {tokensReleased}");
                        _taskExecution.Release(tokensToRelease);
                    }
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
                    _taskKill.Enqueue(true);
                }
            }
        }

        public async Task AllocateThreads(DateTime startTime, CancellationToken ct)
        {
            var endTime = startTime.Add(_config.TestDuration());
            var now = DateTime.UtcNow;
            var lastExecutionRequestCount = _executionRequestCount;
            var lastTotalTokens = _tokensToNow;
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

                    var tokensToNow = _tokensToNow;
                    var executionsToNow = _executionRequestCount;
                    var growth = (tokensToNow - lastTotalTokens) - (executionsToNow - lastExecutionRequestCount);
                    if (growth > 0)
                    {
                        DebugHelper.Write($"token growth occured {growth}");
                        AdjustThreads(startTime, growth, ct);
                    }
                    lastExecutionRequestCount = executionsToNow;
                    lastTotalTokens = tokensToNow;
                }
                else
                {
                    DebugHelper.Write("calculate threads for closed workload");
                    var millisecondsEllapsed = Convert.ToInt32(now.Subtract(startTime).TotalMilliseconds);
                    var desired = _config.Concurrency.Phases.CurrentDesiredThreadCount(millisecondsEllapsed);
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
                        _taskKill.Enqueue(true);
                    }
                }
                DebugHelper.Write($"thread allocation loop complete. thread count {_tasks.Count}");
                await Task.Delay(_config.Engine.ThreadAllocationInterval);
            }
        }

        private bool StartTask(DateTime startTime, CancellationToken ct)
        {
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
                                Result = result.ToString()
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
        }

        private bool IsTestComplete(DateTime startTime, int iterations) =>
            DateTime.UtcNow >= startTime.Add(_config.TestDuration()) || IterationsExceeded(iterations);

        private bool IterationsExceeded(int iterations) =>
            _config.Throughput.Iterations > 0 && iterations > _config.Throughput.Iterations;
    }

    public interface IThreadControl
    {
        Task ReleaseTokens(DateTime startTime, CancellationToken ct);
        Task AllocateThreads(DateTime startTime, CancellationToken ct);
    }
}

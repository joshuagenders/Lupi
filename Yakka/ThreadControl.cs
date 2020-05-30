using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Yakka
{
    public class ThreadControl : IThreadControl
    {
        private readonly SemaphoreSlim _taskExecution;
        private readonly ConcurrentQueue<bool> _taskKill;
        private readonly SemaphoreSlim _taskIncrement;
        private readonly Config _config;
        private readonly IPlugin _plugin;
        private readonly List<Task> _tasks;

        private bool _enabled { get; set; }
        public double _throughput { get; }
        public int _iterations { get; }
        public int _rampUpSeconds { get; }
        public int _holdForSeconds { get; }

        private int _executionRequestCount;

        private int _tokensToNow { get; set; }

        public ThreadControl(Config config, IPlugin plugin)
        {
            _config = config;
            _plugin = plugin;
            _taskExecution = new SemaphoreSlim(0);
            _taskIncrement = new SemaphoreSlim(1);
            _taskKill = new ConcurrentQueue<bool>();
            _tasks = new List<Task>();
            _throughput = config.Throughput.Tps;
            _iterations = config.Throughput.Iterations;
            _rampUpSeconds = Convert.ToInt32(config.Concurrency.RampUp.TotalSeconds);
            _holdForSeconds = Convert.ToInt32(config.Throughput.HoldFor.TotalSeconds);
            _enabled = config.ThroughputEnabled;
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
            DebugHelper.Write($"task execution request start");
            if (_enabled)
            {
                DebugHelper.Write($"task execution enabled, waiting");
                await _taskExecution.WaitAsync(ct);
                DebugHelper.Write($"waiting complete");
            }
            

            if (_taskKill.TryDequeue(out var result))
            {
                return false;
            }

            DebugHelper.Write($"executions {iterations}");
            var isCompleted = IsTestComplete(startTime, iterations);
            DebugHelper.Write($"is test completed {isCompleted} {iterations} {_iterations}");
            return isCompleted;
        }

        public async Task ReleaseTokens(DateTime startTime, CancellationToken ct)
        {
            if (!_enabled)
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
                    if (_iterations > 0)
                    {
                        if (int.MaxValue - tokensToRelease < tokensReleased)
                        {
                            tokensToRelease = int.MaxValue - tokensReleased;
                        }
                        if (tokensToRelease + tokensReleased > _iterations)
                        {
                            tokensToRelease = Convert.ToInt32(_iterations) - tokensReleased;
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
            bool testCompleted = false;
            while (!ct.IsCancellationRequested && !testCompleted)
            {
                DebugHelper.Write($"request task execution {threadName}");
                testCompleted = await RequestTaskExecution(startTime, ct);
                DebugHelper.Write($"task execution request returned {threadName}");
                if (!ct.IsCancellationRequested && !testCompleted)
                {
                    DebugHelper.Write($"test not complete - run method {threadName}");
                    _plugin.ExecuteTestMethod();
                    DebugHelper.Write($"method invoke complete {threadName}");
                }
                else
                {
                    DebugHelper.Write($"thread complete {threadName}");
                    break;
                }
                if (!RequestTaskContinuedExecution())
                {
                    DebugHelper.Write($"continued execution denied. task kill. thread complete {threadName}");
                    break;
                }
                await Task.Delay(_config.Throughput.ThinkTime);
            }
        }

        private bool IsTestComplete(DateTime startTime, int iterations) =>
            DateTime.UtcNow >= startTime.Add(_config.TestDuration()) || IterationsExceeded(iterations);

        private bool IterationsExceeded(int iterations) =>
            _iterations > 0 && iterations > _iterations;
    }

    public interface IThreadControl
    {
        Task ReleaseTokens(DateTime startTime, CancellationToken ct);
        Task AllocateThreads(DateTime startTime, CancellationToken ct);
    }
}

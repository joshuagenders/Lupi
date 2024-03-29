using JustEat.StatsD;
using Lupi.Configuration;
using Microsoft.Extensions.Logging;

namespace Lupi.Core {

    public interface IThreadMarshall
    {
        void AdjustThreadLevels(DateTime startTime, DateTime now, CancellationToken ct);
        int GetThreadCount();
    }

    public class ThreadMarshall : IThreadMarshall
    {
        private readonly List<Task> _tasks;
        private readonly ITestThreadFactory _testThreadFactory;
        private readonly ITokenManager _tokenManager;
        private readonly Config _config;
        private readonly IStatsDPublisher _stats;
        private readonly ILogger<IThreadMarshall> _logger;
        private readonly ILoggerFactory _loggerFactory;

        public ThreadMarshall(
            ITestThreadFactory testThreadFactory,
            ITokenManager tokenManager,
            Config config,
            IStatsDPublisher stats,
            ILogger<IThreadMarshall> logger,
            ILoggerFactory loggerFactory)
        {
            _testThreadFactory = testThreadFactory;
            _tokenManager = tokenManager;
            _config = config;
            _stats = stats;
            _logger = logger;
            _loggerFactory = loggerFactory;
            _tasks = new List<Task>();
        }

        public int GetThreadCount()
        {
            return _tasks.Count;
        }

        public void AdjustThreadLevels(DateTime startTime, DateTime now, CancellationToken ct)
        {
            var threadCount = GetThreadCount();
            if (_config.Concurrency.OpenWorkload)
            {
                _logger.LogDebug("Calculate threads for open workload");
                if (threadCount < _config.Concurrency.MinThreads)
                {
                    SetThreadLevel(_config.Concurrency.MinThreads - threadCount, ct);
                }

                var currentCount = _tokenManager.GetTokenCount();
                if (currentCount > 0)
                {
                    var amount = Math.Max(
                        _config.Concurrency.MinThreads,
                        Math.Min(_config.Concurrency.MaxThreads, threadCount + currentCount));
                    
                    if (amount != threadCount)
                        SetThreadLevel(amount, ct);
                }
            }
            else
            {
                _logger.LogDebug("Calculate threads for closed workload");
                var desired = _config.Concurrency.Phases.CurrentDesiredThreadCount(startTime, now);
                _logger.LogDebug("Desired threads: {desired}. Current threads: {threadCount}", desired, threadCount);
                SetThreadLevel(desired, ct);
            }
        }
        private void SetThreadLevel(int threads, CancellationToken ct)
        {
            _tasks.RemoveAll(x => x.IsCompleted);
            var taskCount = GetThreadCount();
            _stats?.Gauge(taskCount, "threads");
            var difference = threads - taskCount;
            if (difference < 0)
            {
                var tasksToKill = Math.Abs(difference) - _tokenManager.GetTokenCount();
                if (tasksToKill > 0)
                {
                    for (var i = 0; i < tasksToKill; i++)
                    {
                        _tokenManager.RequestTaskDiscontinues();
                    }
                }
            }
            else if (difference > 0)
            {
                _tasks.AddRange(
                    Enumerable.Range(0, difference).Select(_ => 
                        Task.Run(() => _testThreadFactory.GetTestThread().Run(ct), ct)));
            }
            _logger.LogDebug("Task count {threadCount}", taskCount);
        }
    }
}
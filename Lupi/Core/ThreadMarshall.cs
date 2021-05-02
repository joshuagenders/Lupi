using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        private ITestThreadFactory _testThreadFactory;
        private ITokenManager _tokenManager;
        private Config _config;
        private ILogger<IThreadMarshall> _logger;
        private readonly ILoggerFactory _loggerFactory;

        public ThreadMarshall(
            ITestThreadFactory testThreadFactory,
            ITokenManager tokenManager,
            Config config,
            ILogger<IThreadMarshall> logger,
            ILoggerFactory loggerFactory)
        {
            _testThreadFactory = testThreadFactory;
            _tokenManager = tokenManager;
            _config = config;
            _logger = logger;
            _loggerFactory = loggerFactory;
            _tasks = new List<Task>();
        }

        public int GetThreadCount()
        {
            _tasks.RemoveAll(x => x.IsCompleted);
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
                        Math.Min(_config.Concurrency.MaxThreads - threadCount, threadCount + currentCount));
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
            var taskCount = GetThreadCount();
            var difference = threads - taskCount;
            if (difference < 0)
            {
                var tasksToKill = Math.Abs(difference) - _tokenManager.GetTokenCount();
                if (tasksToKill > 0)
                {
                    //todo get from DI
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
            // _stats?.Gauge(threadCount, $"{_config.Listeners.Statsd.Bucket}.threads");
            _logger.LogDebug("Task count {threadCount}", taskCount);
        }
    }
}
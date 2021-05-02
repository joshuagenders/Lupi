using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Lupi.Core {

    public interface IThreadMarshall
    {
        void SetThreadLevel(int threads, CancellationToken ct);
        int GetThreadCount();
    }

    public class ThreadMarshall : IThreadMarshall
    {
        private readonly List<Task> _tasks;
        private readonly ConcurrentQueue<bool> _taskKill;
        private ITestThreadFactory _testThreadFactory;
        private ITokenManager _tokenManager;
        private ILogger<IThreadMarshall> _logger;
        private readonly ILoggerFactory _loggerFactory;

        public ThreadMarshall(
            ITestThreadFactory testThreadFactory,
            ITokenManager tokenManager,
            ILogger<IThreadMarshall> logger,
            ILoggerFactory loggerFactory)
        {
            _testThreadFactory = testThreadFactory;
            _tokenManager = tokenManager;
            _logger = logger;
            _loggerFactory = loggerFactory;
            _taskKill = new ConcurrentQueue<bool>();
            _tasks = new List<Task>();
        }

        public int GetThreadCount()
        {
            _tasks.RemoveAll(x => x.IsCompleted);
            return _tasks.Count;
        }

        public void SetThreadLevel(int threads, CancellationToken ct)
        {
            _tasks.RemoveAll(x => x.IsCompleted);
            var taskCount = _tasks.Count;
            var difference = threads - taskCount;
            if (difference < 0)
            {
                var tasksToKill = Math.Abs(difference) - _taskKill.Count;
                if (tasksToKill > 0)
                {
                    //todo get from DI
                    // _stats?.Increment(tasksToKill, $"{_config.Listeners.Statsd.Bucket}.taskkillrequested");
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
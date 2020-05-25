﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Yakka
{
    public class ThreadAllocator : IThreadAllocator
    {
        private readonly IRunner _testAdapter;
        private readonly IThreadControl _threadControl;
        private readonly List<Task> _tasks;

        public ThreadAllocator(IRunner testAdapter, IThreadControl threadControl)
        {
            _tasks = new List<Task>();
            _testAdapter = testAdapter;
            _threadControl = threadControl;
        }

        public async Task StartThreads(DateTime startTime, int concurrency, int rampUpSeconds, CancellationToken ct)
        {
            //maybe todo - request desired thread state from thread control and adjust to match
            var threadsRemaining = concurrency;
            DebugHelper.Write("start threads");
            while (InRampup(startTime, concurrency, rampUpSeconds) && !ct.IsCancellationRequested)
            {
                var sleepInterval = TimeSpan.FromSeconds(rampUpSeconds / concurrency);
                if (sleepInterval.TotalSeconds > rampUpSeconds)
                {
                    sleepInterval = TimeSpan.FromSeconds(rampUpSeconds);
                }

                if (StartTask(startTime, concurrency, ct))
                {
                    if (--threadsRemaining <= 0)
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
                DebugHelper.Write($"start threads sleeping {sleepInterval}");
                await Task.Delay(sleepInterval, ct);
            }
            DebugHelper.Write($"start threads rampup complete");
            for (var i = 0; i < threadsRemaining; i++)
            {
                if (!StartTask(startTime, concurrency, ct))
                {
                    break;
                }
            }
            DebugHelper.Write($"start threads complete");
        }

        private bool StartTask(DateTime startTime, int concurrency, CancellationToken ct)
        {
            if (_tasks.Count < concurrency)
            {
                _tasks.Add(Task.Run(() => TestLoop(startTime, ct), ct));
                return true;
            }
            return false;
        }

        private async Task TestLoop(DateTime startTime, CancellationToken ct)
        {
            var threadName = $"worker_{Guid.NewGuid().ToString("N")}";
            bool testCompleted = false;
            while (!ct.IsCancellationRequested && !testCompleted)
            {
                DebugHelper.Write($"request task execution");
                testCompleted = await _threadControl.RequestTaskExecution(startTime, ct);
                DebugHelper.Write($"task execution request returned");
                if (!ct.IsCancellationRequested && !testCompleted)
                {
                    DebugHelper.Write($"test not complete - run nunit");
                    _testAdapter.RunTest(threadName);
                    DebugHelper.Write($"nunit run complete");
                }
            }
        }

        private bool InRampup(DateTime startTime, int concurrency, int rampUpSeconds) =>
            concurrency > 1
            && rampUpSeconds > 1
            && startTime.AddSeconds(rampUpSeconds) > DateTime.UtcNow;
    }

    public interface IThreadAllocator
    {
        Task StartThreads(DateTime startTime, int concurrency, int rampUpSeconds, CancellationToken ct);
    }
}

﻿using JustEat.StatsD;
using Lupi.Configuration;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Lupi
{
    public class TestThread
    {
        private readonly IThreadControl _threadControl;
        private readonly ITestResultPublisher _testResultPublisher;
        private readonly StatsDPublisher _stats;
        private readonly Config _config;
        private readonly IPlugin _plugin;

        public TestThread(
            IThreadControl threadControl,
            IPlugin plugin, 
            ITestResultPublisher testResultPublisher,
            StatsDPublisher stats,
            Config config)
        {
            _threadControl = threadControl;
            _plugin = plugin;
            _testResultPublisher = testResultPublisher;
            _stats = stats;
            _config = config;
        }

        public async Task Run(DateTime startTime, CancellationToken ct)
        {
            _stats?.Increment($"{_config.Listeners.Statsd.Bucket}.taskstart");
            var threadName = $"worker {Guid.NewGuid().ToString("N")}";
            bool shouldExit = false;
            var watch = new Stopwatch();
            while (!ct.IsCancellationRequested && !shouldExit)
            {
                DebugHelper.Write($"{threadName} request task execution");
                if (_config.Concurrency.OpenWorkload)
                {
                    var taskExecutionRequest = _threadControl.RequestTaskExecution(startTime, ct);
                    var killDelay = Task.Delay(_config.Concurrency.ThreadIdleKillTime);
                    var result = await Task.WhenAny(taskExecutionRequest, killDelay);
                    if (result == killDelay)
                    {
                        DebugHelper.Write($"{threadName} got tired of waiting. waited {watch.ElapsedMilliseconds}ms then executed. dying.");
                        shouldExit = true;
                        break;
                    }
                    else
                    {
                        shouldExit = await taskExecutionRequest;
                        DebugHelper.Write($"{threadName} task execution request returned. should exit {shouldExit}");
                    }
                }
                else
                {
                    shouldExit = await _threadControl.RequestTaskExecution(startTime, ct);
                    DebugHelper.Write($"{threadName} task execution request returned. should exit {shouldExit}");
                }

                if (!ct.IsCancellationRequested && !shouldExit)
                {
                    DebugHelper.Write($"{threadName} test not complete - run method");
                    object result;
                    try
                    {
                        watch.Restart();
                        result = await _plugin.ExecuteTestMethod();
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

                    DebugHelper.Write($"{threadName} method invoke complete");
                }
                else
                {
                    DebugHelper.Write($"{threadName} thread complete");
                    break;
                }
                await Task.Delay(_config.Throughput.ThinkTime);
            }
            _stats?.Increment($"{_config.Listeners.Statsd.Bucket}.taskcomplete");
        }
    }
}
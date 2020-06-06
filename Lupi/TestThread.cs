using JustEat.StatsD;
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
            var threadName = $"worker_{Guid.NewGuid().ToString("N")}";
            bool shouldExit = false;
            var watch = new Stopwatch();
            while (!ct.IsCancellationRequested && !shouldExit)
            {
                DebugHelper.Write($"request task execution {threadName}");
                if (_config.Concurrency.OpenWorkload)
                {
                    var taskExecutionRequest = _threadControl.RequestTaskExecution(startTime, ct);
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
                    shouldExit = await _threadControl.RequestTaskExecution(startTime, ct);
                    DebugHelper.Write($"task execution request returned {threadName}");
                }

                if (!ct.IsCancellationRequested && !shouldExit)
                {
                    DebugHelper.Write($"test not complete - run method {threadName}");
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
    }
}

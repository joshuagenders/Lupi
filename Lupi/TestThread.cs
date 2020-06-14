using JustEat.StatsD;
using Lupi.Configuration;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger _logger;
        private readonly IPlugin _plugin;

        public TestThread(
            IThreadControl threadControl,
            IPlugin plugin, 
            ITestResultPublisher testResultPublisher,
            StatsDPublisher stats,
            Config config,
            ILogger logger)
        {
            _threadControl = threadControl;
            _plugin = plugin;
            _testResultPublisher = testResultPublisher;
            _stats = stats;
            _config = config;
            _logger = logger;
        }

        private async Task<bool> CanExecute(string threadName, DateTime startTime, CancellationToken ct)
        {
            _logger.LogInformation($"{threadName} request task execution");
            if (_config.Concurrency.OpenWorkload)
            {
                var taskExecutionRequest = _threadControl.RequestTaskExecution(startTime, ct);
                var killDelay = Task.Delay(_config.Concurrency.ThreadIdleKillTime);
                var result = await Task.WhenAny(taskExecutionRequest, killDelay);
                if (result == killDelay)
                {
                    _logger.LogInformation($"{threadName} got tired of waiting. dying.");
                    _stats?.Increment($"{_config.Listeners.Statsd.Bucket}.diedofboredom");
                    return false;
                }
                else
                {
                    return await taskExecutionRequest;
                }
            }
            else
            {
                return await _threadControl.RequestTaskExecution(startTime, ct);
            }
        }

        public async Task Run(DateTime startTime, CancellationToken ct)
        {
            _stats?.Increment($"{_config.Listeners.Statsd.Bucket}.taskstart");
            var threadName = $"worker {Guid.NewGuid().ToString("N")}";
            var watch = new Stopwatch();
            do
            {
                var canExecute = await CanExecute(threadName, startTime, ct);

                if (!ct.IsCancellationRequested && canExecute)
                {
                    _logger.LogInformation($"{threadName} test not complete - run method");
                    object result;
                    try
                    {
                        watch.Restart();
                        result = await _plugin.ExecuteTestMethod();
                        watch.Stop();

                        var duration = result is TimeSpan ? (TimeSpan)result : watch.Elapsed;

                        _testResultPublisher.Publish(
                            new TestResult
                            {
                                Duration = duration,
                                Passed = true,
                                Result = result?.GetType()?.IsValueType ?? false
                                    ? result?.ToString()
                                    : JsonConvert.SerializeObject(result),
                                FinishedTime = DateTime.UtcNow,
                                ThreadName = threadName
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
                                Result = JsonConvert.SerializeObject(ex),
                                FinishedTime = DateTime.UtcNow,
                                ThreadName = threadName
                            });
                    }

                    _logger.LogInformation($"{threadName} method invoke complete");
                }
                else
                {
                    _logger.LogInformation($"{threadName} thread complete");
                    break;
                }

                await Task.Delay(_config.Throughput.ThinkTime);
            } while (!ct.IsCancellationRequested);

            _stats?.Increment($"{_config.Listeners.Statsd.Bucket}.taskcomplete");
        }
    }
}

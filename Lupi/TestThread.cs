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
        private readonly ILogger<TestThread> _logger;
        private readonly IPlugin _plugin;

        public TestThread(
            IThreadControl threadControl,
            IPlugin plugin, 
            ITestResultPublisher testResultPublisher,
            StatsDPublisher stats,
            Config config,
            ILogger<TestThread> logger)
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
            _logger.LogDebug("{threadName} request task execution", threadName);
            if (_config.Concurrency.OpenWorkload)
            {
                var taskExecutionRequest = _threadControl.RequestTaskExecution(startTime, ct);
                var killDelay = Task.Delay(_config.Concurrency.ThreadIdleKillTime);
                var result = await Task.WhenAny(taskExecutionRequest, killDelay);
                if (result == killDelay)
                {
                    _logger.LogDebug("{threadName} got tired of waiting. Dying.", threadName);
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
                    _logger.LogDebug("{threadName} test not complete - run method", threadName);
                    object result;
                    try
                    {
                        watch.Restart();
                        result = await _plugin.ExecuteTestMethod();
                        watch.Stop();

                        ProcessResult(threadName, watch.Elapsed, result);
                    }
                    catch (Exception ex)
                    {
                        watch.Stop();
                        ProcessResult(threadName, watch.Elapsed, ex);
                    }

                    _logger.LogDebug("{threadName} method invoke complete", threadName);
                }
                else
                {
                    _logger.LogDebug("{threadName} thread complete", threadName);
                    break;
                }

                await Task.Delay(_config.Throughput.ThinkTime);
            } while (!ct.IsCancellationRequested);

            _stats?.Increment($"{_config.Listeners.Statsd.Bucket}.taskcomplete");
        }


        private string TrySerialize<T>(T obj)
        {
            try
            {
                return JsonConvert.SerializeObject(obj);
            }
            catch (Exception) 
            {
                return string.Empty;
            }
        }

        private void ProcessResult(string threadName, TimeSpan ellapsed, object taskResult)
        {
            var result = string.Empty;
            var passed = true;
            switch (taskResult)
            {
                case Exception ex:
                    passed = false;
                    result = TrySerialize(ex);
                    break;

                case TimeSpan r:
                    ellapsed = r;
                    break;

                case bool r:
                    passed = r;
                    break;

                case string r:
                    result = r;
                    break;

                case Tuple<TimeSpan, string, bool> r:
                    passed = r.Item3;
                    ellapsed = r.Item1;
                    result = r.Item2;
                    break;
                case Tuple<string, TimeSpan, bool> r:
                    passed = r.Item3;
                    ellapsed = r.Item2;
                    result = r.Item1;
                    break;
                case Tuple<string, bool, TimeSpan> r:
                    passed = r.Item2;
                    ellapsed = r.Item3;
                    result = r.Item1;
                    break;
                case Tuple<bool, string, TimeSpan> r:
                    passed = r.Item1;
                    ellapsed = r.Item3;
                    result = r.Item2;
                    break;
                case Tuple<TimeSpan, bool, string> r:
                    passed = r.Item2;
                    ellapsed = r.Item1;
                    result = r.Item3;
                    break;

                case Tuple<TimeSpan, bool> r:
                    passed = r.Item2;
                    ellapsed = r.Item1;
                    break;
                case Tuple<bool, TimeSpan> r:
                    passed = r.Item1;
                    ellapsed = r.Item2;
                    break;

                case Tuple<TimeSpan, string> r:
                    ellapsed = r.Item1;
                    result = r.Item2;
                    break;
                case Tuple<string, TimeSpan> r:
                    ellapsed = r.Item2;
                    result = r.Item1;
                    break;

                case Tuple<bool, string> r:
                    passed = r.Item1;
                    result = r.Item2;
                    break;
                case Tuple<string, bool> r:
                    passed = r.Item2;
                    result = r.Item1;
                    break;

                case ValueTuple<TimeSpan, string, bool> r:
                    passed = r.Item3;
                    ellapsed = r.Item1;
                    result = r.Item2;
                    break;
                case ValueTuple<string, TimeSpan, bool> r:
                    passed = r.Item3;
                    ellapsed = r.Item2;
                    result = r.Item1;
                    break;
                case ValueTuple<string, bool, TimeSpan> r:
                    passed = r.Item2;
                    ellapsed = r.Item3;
                    result = r.Item1;
                    break;
                case ValueTuple<bool, string, TimeSpan> r:
                    passed = r.Item1;
                    ellapsed = r.Item3;
                    result = r.Item2;
                    break;
                case ValueTuple<TimeSpan, bool, string> r:
                    passed = r.Item2;
                    ellapsed = r.Item1;
                    result = r.Item3;
                    break;

                case ValueTuple<TimeSpan, bool> r:
                    passed = r.Item2;
                    ellapsed = r.Item1;
                    break;
                case ValueTuple<bool, TimeSpan> r:
                    passed = r.Item1;
                    ellapsed = r.Item2;
                    break;

                case ValueTuple<TimeSpan, string> r:
                    ellapsed = r.Item1;
                    result = r.Item2;
                    break;
                case ValueTuple<string, TimeSpan> r:
                    ellapsed = r.Item2;
                    result = r.Item1;
                    break;

                case ValueTuple<bool, string> r:
                    passed = r.Item1;
                    result = r.Item2;
                    break;
                case ValueTuple<string, bool> r:
                    passed = r.Item2;
                    result = r.Item1;
                    break;

                default:
                    result = taskResult?.GetType()?.IsValueType ?? false
                        ? taskResult?.ToString()
                        : TrySerialize(taskResult);
                    break;
            }

            _testResultPublisher.Publish(
                new TestResult
                {
                    Duration = ellapsed,
                    Passed = passed,
                    Result = result,
                    FinishedTime = DateTime.UtcNow,
                    ThreadName = threadName
                });
        }
    }
}

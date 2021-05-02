using System;
using System.Threading;
using System.Threading.Tasks;
using Lupi.Configuration;
using JustEat.StatsD;
using Microsoft.Extensions.Logging;
using Lupi.Services;

namespace Lupi.Core
{
    public class ThreadControl : IThreadControl
    {
        private readonly Config _config;
        private readonly IThreadMarshall _threadMarshall;
        private readonly ITokenManager _tokenManager;
        private readonly ITimeService _timeService;
        private readonly ILogger<IThreadControl> _logger;

        private readonly IStatsDPublisher _stats;


        private DateTime _startTime;
        private DateTime _endTime;
        private DateTime _now;

        public ThreadControl(
            Config config, 
            IThreadMarshall threadMarshall,
            ITokenManager tokenManager,
            ITimeService timeService,
            ILogger<IThreadControl> logger)
        {
            _config = config;
            _threadMarshall = threadMarshall;
            _timeService = timeService;
            _logger = logger;
            // todo from DI
            // if (_config.Listeners.ActiveListeners.Contains("statsd"))
            // {
            //     _stats = new StatsDPublisher(new StatsDConfiguration
            //     {
            //         Host = _config.Listeners.Statsd.Host,
            //         Port = _config.Listeners.Statsd.Port,
            //         Prefix = _config.Listeners.Statsd.Prefix
            //     });
            // }
        }

        public async Task Run(CancellationToken ct)
        {
            //todo move up into what calls run
            // _logger.LogInformation("Executing setup method");
            // await _plugin.ExecuteSetupMethod();
            try 
            {
                _startTime = _timeService.Now();
                _endTime = _startTime.Add(_config.TestDuration());
                _now = _startTime;
                _tokenManager.Initialise(_startTime, _endTime);
                _logger.LogInformation("Beginning main test loop");
                _logger.LogInformation("Test start time: {startTime}", _startTime);
                _logger.LogInformation("Test end time: {endTime}", _endTime);
                while (_now < _endTime && !ct.IsCancellationRequested)
                {
                    _tokenManager.ReleaseTokens(_now);
                    AdjustThreadLevels(ct);

                    var threadCount = _threadMarshall.GetThreadCount();
                    _logger.LogDebug("Main loop complete. thread count {threadCount}", threadCount);
                    _stats?.Gauge(threadCount, $"{_config.Listeners.Statsd.Bucket}.threads");

                    await Task.Delay(_config.Engine.CheckInterval, ct);
                    _now = _timeService.Now();
                }
            }
            finally
            {
                //todo move up into what calls run
                // _logger.LogInformation("Main test loop completed");
                // _logger.LogInformation("Executing teardown method");
                // _stats?.Gauge(0, $"{_config.Listeners.Statsd.Bucket}.threads");
                // await _plugin.ExecuteTeardownMethod();
            }
        }

        private void AdjustThreadLevels(CancellationToken ct)
        {
            var threadCount = _threadMarshall.GetThreadCount();
            if (_config.Concurrency.OpenWorkload)
            {
                _logger.LogDebug("Calculate threads for open workload");
                if (threadCount < _config.Concurrency.MinThreads)
                {
                    _threadMarshall.SetThreadLevel(_config.Concurrency.MinThreads - threadCount, ct);
                }

                var currentCount = _tokenManager.GetTokenCount();
                if (currentCount > 0)
                {
                    var amount = Math.Max(
                        _config.Concurrency.MinThreads,
                        Math.Min(_config.Concurrency.MaxThreads - threadCount, threadCount + currentCount));
                    _threadMarshall.SetThreadLevel(amount, ct);
                }
            }
            else
            {
                _logger.LogDebug("Calculate threads for closed workload");
                var desired = _config.Concurrency.Phases.CurrentDesiredThreadCount(_startTime, _now);
                _logger.LogDebug("Desired threads: {desired}. Current threads: {threadCount}", desired, threadCount);
                _threadMarshall.SetThreadLevel(desired, ct);
            }
        }
    }

    public interface IThreadControl
    {
        Task Run(CancellationToken ct);
    }
}

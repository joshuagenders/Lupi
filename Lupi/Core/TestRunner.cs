using System;
using System.Threading;
using System.Threading.Tasks;
using Lupi.Configuration;
using JustEat.StatsD;
using Microsoft.Extensions.Logging;
using Lupi.Services;

namespace Lupi.Core
{
    public class TestRunner : ITestRunner //todo - rename? is essentially the core loop
    {
        private readonly Config _config;
        private readonly IThreadMarshall _threadMarshall;
        private readonly ITokenManager _tokenManager;
        private readonly ITimeService _timeService;
        private readonly ISleepService _sleepService;
        private readonly ILogger<ITestRunner> _logger;

        private readonly IStatsDPublisher _stats;


        private DateTime _startTime;
        private DateTime _endTime;
        private DateTime _now;

        public TestRunner(
            Config config, 
            IThreadMarshall threadMarshall,
            ITokenManager tokenManager,
            ITimeService timeService,
            ISleepService sleepService,
            IStatsDPublisher stats,
            ILogger<ITestRunner> logger)
        {
            _config = config;
            _threadMarshall = threadMarshall;
            _tokenManager = tokenManager;
            _timeService = timeService;
            _sleepService = sleepService;
            _stats = stats;
            _logger = logger;
        }

        public async Task Run(CancellationToken ct)
        {
            //todo move up into what calls run?
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
                    _threadMarshall.AdjustThreadLevels(_startTime, _now, ct);

                    await _sleepService.WaitFor(_config.Engine.CheckInterval, ct);
                    _now = _timeService.Now();
                }
            }
            finally
            {
                //todo move up into what calls run?
                _logger.LogInformation("Main test loop completed");
                _logger.LogInformation("Executing teardown method");
                _stats?.Gauge(0, "threads");
                // await _plugin.ExecuteTeardownMethod();
            }   
        }
    }

    public interface ITestRunner
    {
        Task Run(CancellationToken ct);
    }
}

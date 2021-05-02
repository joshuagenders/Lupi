using JustEat.StatsD;
using Lupi.Configuration;
using Lupi.Results;
using Lupi.Services;
using Microsoft.Extensions.Logging;

namespace Lupi.Core {
    public interface ITestThreadFactory
    {
        TestThread GetTestThread();
    }

    public class TestThreadFactory : ITestThreadFactory 
    {
        private readonly Config _config;
        private readonly ITokenManager _tokenManager;
        private readonly IPlugin _plugin;
        private readonly ITestResultPublisher _testResultPublisher;
        private readonly ITimeService _timeService;
        private readonly IStopwatchFactory _stopwatchFactory;
        private readonly ILogger<ITestThreadFactory> _logger;
        private ILoggerFactory _loggerFactory;
        private IStatsDPublisher _stats;

        public TestThreadFactory(
            Config config,
            ITokenManager tokenManager,
            IPlugin plugin, 
            ITestResultPublisher testResultPublisher,
            ITimeService timeService,
            IStopwatchFactory stopwatchFactory, 
            ILogger<ITestThreadFactory> logger,
            ILoggerFactory loggerFactory)
        {
            _config = config;
            _tokenManager = tokenManager;
            _plugin = plugin;
            _testResultPublisher = testResultPublisher;
            _timeService = timeService;
            _stopwatchFactory = stopwatchFactory;
            _logger = logger;
            _loggerFactory = loggerFactory;

            if (_config.Listeners.ActiveListeners.Contains("statsd"))
            {
                _stats = new StatsDPublisher(new StatsDConfiguration
                {
                    Host = _config.Listeners.Statsd.Host,
                    Port = _config.Listeners.Statsd.Port,
                    Prefix = _config.Listeners.Statsd.Prefix
                });
            }
        }

        public TestThread GetTestThread()
        {
            return new TestThread(
                _tokenManager,
                _plugin,
                _testResultPublisher,
                _stats,
                _config,
                _timeService,
                _stopwatchFactory, 
                _loggerFactory.CreateLogger<TestThread>());
        }
    }
}
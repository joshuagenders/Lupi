using JustEat.StatsD;
using System;
using System.Threading;
using System.Threading.Tasks;
using Lupi.Configuration;

namespace Lupi.Listeners
{
    public class StatsdListener : ITestResultListener
    {
        private readonly Config _config;
        private readonly IStatsDPublisher _client;

        public StatsdListener(Config config)
        {
            _config = config;
            _client = new StatsDPublisher(new StatsDConfiguration
            {
                Host = _config.Listeners.Statsd.Host,
                Port = _config.Listeners.Statsd.Port,
                Prefix = _config.Listeners.Statsd.Prefix
            });
        }

        public async Task OnResult(TestResult[] results, CancellationToken ct)
        {
            foreach (var result in results)
            {
                var passFail = result.Passed ? "success" : "failure";
                var bucket = $"{_config.Listeners.Statsd.Bucket}.{passFail}";
                _client.Timing(Convert.ToInt32(result.Duration.TotalMilliseconds), bucket);
            }

            await Task.CompletedTask;
        }
    }
}

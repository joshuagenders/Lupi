using JustEat.StatsD;
using Lupi.Configuration;
using Lupi.Results;

namespace Lupi.Listeners
{
    public class StatsdListener : ITestResultListener
    {
        private readonly Config _config;
        private readonly IStatsDPublisher _stats;

        public StatsdListener(Config config, IStatsDPublisher stats)
        {
            _config = config;
            _stats = stats;
        }

        public async Task OnResult(TestResult[] results, CancellationToken ct)
        {
            foreach (var result in results)
            {
                var bucket = result.Passed ? "success" : "failure";
                _stats?.Timing(Convert.ToInt32(result.Duration.TotalMilliseconds), bucket);
            }

            await Task.CompletedTask;
        }
    }
}

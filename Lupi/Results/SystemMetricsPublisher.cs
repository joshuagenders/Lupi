using JustEat.StatsD;
using Lupi.Configuration;
using System.Diagnostics;

namespace Lupi.Results
{
    public interface ISystemMetricsPublisher : IDisposable
    {
        public Task Process(CancellationToken ct);
        public bool TestCompleted { get; set; }
    }

    public class SystemMetricsPublisher : ISystemMetricsPublisher
    {
        private readonly PerformanceCounter[] _counters;
        private readonly Config _config;
        private readonly IStatsDPublisher _stats;
        private readonly IHttpEventListener _httpEventListener;
        public bool TestCompleted { get; set; }

        public SystemMetricsPublisher(IHttpEventListener httpEventListener, IStatsDPublisher stats, Config config)
        {
            if (OperatingSystem.IsWindows())
            {
                _counters = PerformanceCounterCategory.GetCategories()
                    .Where(category => category.CategoryName.Equals("Processor") || category.CategoryName.Equals("Memory"))
                    .SelectMany(category => category.GetInstanceNames().Select(instanceName => new { instanceName, category }))
                    .SelectMany(o => o.category.GetCounters(o.instanceName))
                    .ToArray();
            }

            _config = config;
            _stats = stats;
            _httpEventListener = httpEventListener;
        }

        public async Task Process(CancellationToken ct)
        {
            if (_stats == null || !(_counters?.Any() ?? false))
            {
                return;
            }
            while (!TestCompleted && !ct.IsCancellationRequested)
            {
                foreach (var c in _counters)
                {
                    _stats?.Gauge(c.NextValue(), $"system.{c.CounterName}");
                }
                await Task.Delay(5000, ct);
            }
        }

        public void Dispose()
        {
            ((IDisposable)_httpEventListener)?.Dispose();
        }
    }
}

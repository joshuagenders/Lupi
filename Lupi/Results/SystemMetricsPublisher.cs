using JustEat.StatsD;
using Lupi.Configuration;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

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
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _counters = PerformanceCounterCategory.GetCategories()
                    .Where(cat => cat.CategoryName.Equals("Processor") || cat.CategoryName.Equals("Memory"))
                    .SelectMany(cat => cat.GetCounters())
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

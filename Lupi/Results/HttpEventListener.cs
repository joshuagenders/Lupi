using System.Diagnostics.Tracing;
using JustEat.StatsD;
using Lupi.Configuration;

namespace Lupi.Results
{
    public sealed class HttpEventListener : EventListener, IHttpEventListener
    {
        private readonly Config _config;
        private readonly IStatsDPublisher _stats;

        public HttpEventListener(Config config, IStatsDPublisher stats){
            _config = config;
            _stats = stats;
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            // List of event source names provided by networking in .NET 5.
            if ((_config?.Listeners?.ActiveListeners?.Contains("statsd") ?? false) &&
                (eventSource.Name == "System.Net.Http" ||
                eventSource.Name == "System.Net.Sockets" ||
                eventSource.Name == "System.Net.Security" ||
                eventSource.Name == "System.Net.NameResolution"))
            {
                EnableEvents(eventSource, EventLevel.LogAlways, EventKeywords.All, new Dictionary<string, string>()
                {
                    // These additional arguments will turn on counters monitoring with a reporting interval set to a half of a second. 
                    ["EventCounterIntervalSec"] = TimeSpan.FromSeconds(0.5).TotalSeconds.ToString()
                });
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            // It's a counter, parse the data properly.
            if (eventData.EventId == -1)
            {
                var counterPayload = (IDictionary<string, object>)(eventData.Payload[0]);
                if (counterPayload == null) return;

                switch(counterPayload["CounterType"]){
                    case "Mean":
                        _stats?.Gauge(Double.Parse(counterPayload["Count"].ToString()),$"http.{counterPayload["Name"]}");
                        break;
                    case "Sum":
                         _stats?.Increment(long.Parse(counterPayload["Increment"].ToString()), $"http.{counterPayload["Name"]}");
                         break;
                    default:
                        break;
                }
            }
        }
    }

    public interface IHttpEventListener
    {
    }
}
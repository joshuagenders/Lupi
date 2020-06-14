using System;
using System.Threading;
using System.Threading.Tasks;
using Lupi.Configuration;

namespace Lupi.Listeners
{
    public class ConsoleAggregatorListener : IAggregatorListener
    {
        private readonly Config _config;

        public ConsoleAggregatorListener(Config config)
        {
            _config = config;
            Console.WriteLine("Console Listener");
            Console.WriteLine("----------------");
        }
        public async Task OnResult(AggregatedResult result, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(_config.Listeners.Console.Format))
            {
                Console.WriteLine("----------------");
                Console.WriteLine($"Moving Average: {result.MovingAverage}ms, Min: {result.Min}ms, Max: {result.Max}ms");
                Console.WriteLine($"Period Average: {result.PeriodAverage}ms, Period Min: {result.PeriodMin}ms, Period Max: {result.PeriodMax}ms");
                Console.WriteLine($"Period Length: {result.PeriodLength}ms, Sample Count: {result.Count}");
                Console.WriteLine($"Success: {result.PeriodSuccessCount} Failure: {result.PeriodErrorCount}.");
            }
            else
            {
                Console.WriteLine(_config.Listeners.Console.Format.FormatWith(result));
            }
            await Task.CompletedTask;
        }
    }
}

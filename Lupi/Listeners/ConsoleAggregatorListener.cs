using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lupi.Listeners
{
    public class ConsoleAggregatorListener : IAggregatorListener
    {
        public ConsoleAggregatorListener()
        {
            Console.WriteLine("Console Listener");
            Console.WriteLine("----------------");
        }
        public async Task OnResult(AggregatedResult result, CancellationToken ct)
        {
            Console.WriteLine($"Moving Average: {result.MovingAverage}ms, Min: {result.Min}ms, Max: {result.Max}ms");
            Console.WriteLine($"Period Average: {result.PeriodAverage}ms, Period Min: {result.PeriodMin}ms, Period Max: {result.PeriodMax}ms");
            Console.WriteLine($"Period Length: {result.PeriodLength}ms, Sample Count: {result.Count}");
            Console.WriteLine($"Success:{result.PeriodSuccessCount} Error: {result.PeriodErrorCount}.");
            await Task.CompletedTask;
        }
    }
}

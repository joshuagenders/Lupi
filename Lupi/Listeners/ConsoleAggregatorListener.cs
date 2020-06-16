using System;
using System.Threading;
using System.Threading.Tasks;
using Lupi.Configuration;

namespace Lupi.Listeners
{
    public class ConsoleAggregatorListener : IAggregatorListener
    {
        private readonly Config _config;

        private const string DefaultFormat = @"
----------------

Mean:           {Mean,10:N}ms  Std dev:   {StandardDeviation,10:N}
Moving Average: {MovingAverage,10:N}ms  Min:        {Min,10:N}ms  Max:        {Max,10:N}ms
Period Average: {PeriodAverage,10:N}ms  Period Min: {PeriodMin,10:N}ms  Period Max: {PeriodMax,10:N}ms
Period Length:  {PeriodLength,10:N}ms

Sample Count: {Count,5}  Success: {PeriodSuccessCount,5}  Failure: {PeriodErrorCount,5}.";

        public ConsoleAggregatorListener(Config config)
        {
            _config = config;
        }

        public async Task OnResult(AggregatedResult result, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(_config.Listeners.Console.Format))
            {
                Console.WriteLine(DefaultFormat.FormatWith(result));
            }
            else
            {
                Console.WriteLine(_config.Listeners.Console.Format.FormatWith(result));
            }
            await Task.CompletedTask;
        }
    }
}

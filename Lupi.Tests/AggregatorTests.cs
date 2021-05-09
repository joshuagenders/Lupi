using System;
using System.Threading;
using System.Threading.Tasks;
using Lupi.Listeners;
using Moq;
using Xunit;

namespace Lupi.Tests 
{
    public class AggregatorTests 
    {
        [Theory]
        [AutoMoqData]
        public async Task WhenSingleResultAggregated_ThenResultIsUnchanged(
            Mock<IAggregatorListener> aggregatorListener,
            Aggregator aggregator)
        {
            var cts = new CancellationTokenSource();
            var duration = TimeSpan.FromMilliseconds(2341);
            aggregator.Subscribe(aggregatorListener.Object);
            await aggregator.OnResult(
                new Results.TestResult[] {
                    new Results.TestResult {
                        Duration = duration,
                        FinishedTime = DateTime.Now,
                        Passed = true,
                        ThreadName = "threadname"
                    }
                }, cts.Token);
            var processTask = aggregator.Process(cts.Token);
            cts.CancelAfter(100);
            await processTask;
            aggregatorListener.Verify(a => a.OnResult(It.Is<AggregatedResult>(r => 
                r.Count == 1
                && r.Max == duration.TotalMilliseconds
                && r.Mean == duration.TotalMilliseconds
                && r.Min == duration.TotalMilliseconds
                && r.PeriodSuccessCount == 1
            ), It.IsAny<CancellationToken>()));
        }

        // todo more cases, parameterise
        [Theory]
        [AutoMoqData]
        public async Task WhenManyResultsAggregated_ThenListenersAreSentCorrectMetrics(
            Mock<IAggregatorListener> aggregatorListener,
            Aggregator aggregator)
        {
            var cts = new CancellationTokenSource();
            var duration = TimeSpan.FromMilliseconds(2);
            aggregator.Subscribe(aggregatorListener.Object);
            var result = new Results.TestResult {
                Duration = duration,
                FinishedTime = DateTime.Now,
                Passed = true,
                ThreadName = "threadname"
            };

            await aggregator.OnResult(
                new Results.TestResult[] {
                   result, result, result, result
                }, cts.Token);

            var processTask = aggregator.Process(cts.Token);
            cts.CancelAfter(100);
            await processTask;
            aggregatorListener.Verify(a => a.OnResult(It.Is<AggregatedResult>(r => 
                r.Count == 4
                && r.Max == duration.TotalMilliseconds
                && r.Mean == duration.TotalMilliseconds
                && r.Min == duration.TotalMilliseconds
                && r.PeriodSuccessCount == 4
            ), It.IsAny<CancellationToken>()));
        }

        [Theory]
        [AutoMoqData]
        public async Task WhenTestIsCompleted_RemainingResultsAreAggregated(
            Mock<IAggregatorListener> aggregatorListener,
            Aggregator aggregator)
        {
            var cts = new CancellationTokenSource();
            aggregator.Subscribe(aggregatorListener.Object);
            await aggregator.OnResult(
                new Results.TestResult[] {
                    new Results.TestResult {
                        Duration = TimeSpan.FromMilliseconds(2341),
                        FinishedTime = DateTime.Now,
                        Passed = true,
                        ThreadName = "threadname"
                    }
                }, cts.Token);
            cts.Cancel();
            try 
            {
                await aggregator.Process(cts.Token);
            }
            catch(TaskCanceledException){}
            finally 
            {
                aggregatorListener.Verify(a => a.OnResult(It.IsAny<AggregatedResult>(), It.IsAny<CancellationToken>()));
            }
        }
    }

}
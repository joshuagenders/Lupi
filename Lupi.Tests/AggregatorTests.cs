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
        // todo
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

        [Fact]
        public void WhenManyResultsAggregated_ThenListenersAreSentCorrectMetrics()
        {
            throw new NotImplementedException();
        }


        [Fact]
        public void WhenTestIsCompleted_RemainingResultsAreAggregated()
        {
            throw new NotImplementedException();
        }
    }

}
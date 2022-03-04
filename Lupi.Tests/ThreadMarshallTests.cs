using Lupi.Configuration;
using Lupi.Core;

namespace Lupi.Tests
{
    public class ThreadMarshallTests
    {
        [Theory]
        [AutoMoqData]
        public async void WhenConcurrencySpecifiedInClosedWorkload_ThenThreadLevelSet(
            [Frozen] Mock<ITokenManager> tokenManager,
            [Frozen] Config config,
            ThreadMarshall threadMarshall)
        {
            var cts = new CancellationTokenSource();
            var startTime = new DateTime(2020, 05, 02, 13, 0, 0);
            var endTime = startTime.AddMinutes(5);
            config.Concurrency.RampUp = TimeSpan.FromMinutes(1);
            config.Concurrency.Threads = 10;
            config.Concurrency.HoldFor = TimeSpan.FromMinutes(3);
            config.Concurrency.RampDown = TimeSpan.FromMinutes(1);
            config.Concurrency.OpenWorkload = false;
            config.Concurrency.Phases = config.BuildStandardConcurrencyPhases();
            threadMarshall.AdjustThreadLevels(startTime, startTime.AddSeconds(30), cts.Token);
            await Task.Delay(1);
            threadMarshall.GetThreadCount().Should().BeCloseTo(config.Concurrency.Threads / 2, 1);

            threadMarshall.AdjustThreadLevels(startTime, startTime.AddMinutes(2), cts.Token);
            await Task.Delay(1);
            threadMarshall.GetThreadCount().Should().BeCloseTo(config.Concurrency.Threads, 1);

            threadMarshall.AdjustThreadLevels(startTime, startTime.AddMinutes(4).AddSeconds(30), cts.Token);
            await Task.Delay(1);
            tokenManager.Verify(
                t => t.RequestTaskDiscontinues(),
                Times.Between((int)(config.Concurrency.Threads / 2 - 1), (int)(config.Concurrency.Threads / 2 + 1), Moq.Range.Inclusive));
        }

        [Theory]
        [AutoMoqData]
        public async Task WhenConcurrencySpecifiedInOpenWorkload_ThenThreadLevelAdjusts(
            [Frozen] Mock<ITestThreadFactory> testThreadFactory,
            Mock<TestThread> testThread,
            [Frozen] Mock<ITokenManager> tokenManager,
            [Frozen] Config config,
            ThreadMarshall threadMarshall)
        {
            var cts = new CancellationTokenSource();
            var startTime = new DateTime(2020, 05, 02, 13, 0, 0);
            var endTime = startTime.AddMinutes(5);
            config.Concurrency.OpenWorkload = true;
            config.Concurrency.MinThreads = 10;
            config.Concurrency.MaxThreads = 15;
            config.Concurrency.Phases = new System.Collections.Generic.List<ConcurrencyPhase>();
            config.Throughput.Tps = 1;

            testThread.Setup(t => t.Run(It.IsAny<CancellationToken>())).Returns(Task.Delay(TimeSpan.FromSeconds(30)));
            testThreadFactory.Setup(t => t.GetTestThread()).Returns(testThread.Object);
            threadMarshall.AdjustThreadLevels(startTime, startTime, cts.Token);
            await Task.Delay(1);
            threadMarshall.GetThreadCount().Should().Be(10);

            tokenManager.Setup(t => t.GetTokenCount()).Returns(1);
            threadMarshall.AdjustThreadLevels(startTime, startTime.AddSeconds(10), cts.Token);
            await Task.Delay(1);
            threadMarshall.GetThreadCount().Should().Be(11);
        }
    }
}
using FluentAssertions;
using Moq;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Yakka.Tests
{
    public class ApplicationTests
    {
        [Theory]
        [InlineAutoMoqData(1, 0, 0, 2, 1)]
        [InlineAutoMoqData(2, 0, 0, 2, 2)]
        [InlineAutoMoqData(4, 0, 0, 2, 20)]
        [InlineAutoMoqData(3, 0, 0, 2, 1)]
        [InlineAutoMoqData(1, 0, 0, 2, 2)]
        [InlineAutoMoqData(1, 1, 0, 2, 1)]
        [InlineAutoMoqData(2, 1, 0, 2, 2)]
        [InlineAutoMoqData(3, 1, 0, 2, 1)]
        [InlineAutoMoqData(1, 1, 0, 2, 2)]
        [InlineAutoMoqData(4, 0, 1, 2, 1)]
        [InlineAutoMoqData(4, 1, 1, 2, 2)]
        [InlineAutoMoqData(2, 0.9, 0, 6, 1)]

        public async Task WhenIterationsSpecified_ThenIterationsAreNotExceeded(
            int concurrency,
            double throughput,
            int rampUpSeconds,
            int holdForSeconds,
            int iterations,
            Mock<IRunner> runner)
        {
            var config = GetConfig(concurrency, throughput, rampUpSeconds, holdForSeconds, iterations);
            var cts = new CancellationTokenSource();
            var threadControl = new ThreadControl(config);
            var threadAllocator = new ThreadAllocator(runner.Object, threadControl);
            var app = new Application(threadAllocator, threadControl, config);

            cts.CancelAfter(TimeSpan.FromSeconds(holdForSeconds + rampUpSeconds + 1));
            await app.Run(cts.Token);
            // why this fails I have no idea
            //await RunApp(concurrency, throughput, iterations: 0, rampUpSeconds, holdForSeconds, runner.Object);
            
            runner.Verify(n =>
                n.RunTest(It.IsRegex("worker_.+")),
                Times.Exactly(iterations));
        }

        [Theory]
        [InlineAutoMoqData(1, 0, 0, 2)]
        [InlineAutoMoqData(2, 0, 2, 2)]
        [InlineAutoMoqData(3, 0, 2, 0)]
        [InlineAutoMoqData(2, 1, 0, 2)]
        [InlineAutoMoqData(3, 1, 2, 2)]
        [InlineAutoMoqData(4, 1, 2, 0)]
        public async Task WhenDurationSpecified_ThenDurationIsObserved(
            int concurrency,
            double throughput,
            int rampUpSeconds,
            int holdForSeconds,
            Mock<IRunner> runner)
        {
            var watch = new Stopwatch();
            watch.Start();
            await RunApp(concurrency, throughput, iterations: 0, rampUpSeconds, holdForSeconds, runner.Object);
            watch.Stop();
            watch.Elapsed.Should().BeGreaterOrEqualTo(
                TimeSpan.FromSeconds(holdForSeconds + rampUpSeconds)
                        .Subtract(TimeSpan.FromMilliseconds(10)));
        }

        [Theory]
        [InlineAutoMoqData(3, 1, 2, 3, 25)]
        [InlineAutoMoqData(1, 0, 2, 2, 1000)]
        [InlineAutoMoqData(3, 1, 0, 3, 25)]
        [InlineAutoMoqData(1, 0, 0, 2, 1000)]
        public async Task WhenMoreIterationsThanDurationAllows_ThenTestExitsEarly(
            int concurrency,
            double throughput,
            int rampUpSeconds,
            int holdForSeconds,
            int iterations)
        {
            var runner = new runnerFake();
            await RunApp(concurrency, throughput, iterations: 0, rampUpSeconds, holdForSeconds, runner);
            runner.Calls.Should().BeLessThan(iterations);
        }

        [Theory]
        [InlineAutoMoqData(1, 1, 0, 4)]
        [InlineAutoMoqData(2, 1, 0, 4)]
        [InlineAutoMoqData(1, 0.8, 0, 4)]
        [InlineAutoMoqData(2, 2, 0, 3)]
        [InlineAutoMoqData(2, 20, 0, 4)]
        [InlineAutoMoqData(1, 1, 2, 5)]
        [InlineAutoMoqData(2, 1, 2, 4)]
        [InlineAutoMoqData(1, 0.8, 2, 5)]
        [InlineAutoMoqData(2, 2, 2, 2)]
        public async Task WhenThroughputIsSpecified_ThenRPSIsNotExceeded(
            int concurrency,
            double throughput,
            int rampUpSeconds,
            int holdForSeconds,
            Mock<IRunner> runner)
        {
            await RunApp(concurrency, throughput, iterations: 0, rampUpSeconds, holdForSeconds, runner.Object);

            var expectedTotal = throughput * holdForSeconds +
                (rampUpSeconds * throughput / 2);
            var tps = concurrency * throughput;

            runner.Verify(n =>
                n.RunTest(It.IsRegex("worker_.+")),
                Times.Between(Convert.ToInt32(expectedTotal - tps), Convert.ToInt32(expectedTotal), Moq.Range.Inclusive));
        }

        private async Task RunApp(
            int concurrency,
            double throughput,
            int iterations,
            int rampUpSeconds,
            int holdForSeconds,
            IRunner runner)
        {
            var cts = new CancellationTokenSource();
            var config = GetConfig(concurrency, throughput, rampUpSeconds, holdForSeconds, iterations);
            var threadControl = new ThreadControl(config);
            var threadAllocator = new ThreadAllocator(runner, threadControl);
            var app = new Application(threadAllocator, threadControl, config);

            cts.CancelAfter(TimeSpan.FromSeconds(holdForSeconds + rampUpSeconds + 1));
            await app.Run(cts.Token);
        }

        private Config GetConfig(
            int concurrency,
            double throughput,
            int rampUpSeconds,
            int holdForSeconds,
            int iterations) =>
            new Config
            {
                Concurrency = new Concurrency
                {
                    Threads = concurrency,
                    RampUp = TimeSpan.FromSeconds(rampUpSeconds)
                },
                Throughput = new Throughput
                {
                    HoldFor = TimeSpan.FromSeconds(holdForSeconds),
                    RampUp = TimeSpan.FromSeconds(rampUpSeconds),
                    Iterations = iterations,
                    Tps = throughput
                }
            };

        class runnerFake : IRunner
        {
            public int Calls;
            
            public void RunTest(string threadName)
            {
                Interlocked.Increment(ref Calls);
                Thread.Sleep(400);
            }
        }
    }
}

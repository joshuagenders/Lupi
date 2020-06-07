using FluentAssertions;
using Moq;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Lupi.Configuration;

namespace Lupi.Tests
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
        [InlineAutoMoqData(2, 1, 0, 3, 2)]
        [InlineAutoMoqData(3, 1, 0, 2, 1)]
        [InlineAutoMoqData(1, 1, 0, 3, 2)]
        [InlineAutoMoqData(4, 0, 1, 2, 1)]
        [InlineAutoMoqData(4, 1, 1, 3, 2)]
        [InlineAutoMoqData(2, 0.9, 0, 6, 1)]

        public async Task WhenIterationsSpecified_ThenIterationsAreNotExceeded(
            int concurrency,
            double throughput,
            int rampUpSeconds,
            int holdForSeconds,
            int iterations,
            Mock<IPlugin> plugin,
            Mock<ITestResultPublisher> testResultPublisher)
        {
            var config = GetConfig(concurrency, throughput, rampUpSeconds, holdForSeconds, iterations);
            await RunApp(config, plugin.Object, testResultPublisher.Object);
            
            plugin.Verify(n => n.ExecuteTestMethod(), Times.Exactly(iterations));
            testResultPublisher.Verify(s => s.Publish(It.IsAny<TestResult>()), Times.Exactly(iterations));
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
            Mock<IPlugin> plugin,
            Mock<ITestResultPublisher> testResultPublisher)
        {
            var watch = new Stopwatch();
            watch.Start();
            var config = GetConfig(
                concurrency: concurrency, 
                throughput: throughput, 
                rampUpSeconds: rampUpSeconds, 
                holdForSeconds: holdForSeconds);

            await RunApp(config, plugin.Object, testResultPublisher.Object);
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
            int iterations,
            Mock<ITestResultPublisher> testResultPublisher)
        {
            var plugin = new PluginFake();
            var config = GetConfig(
                concurrency: concurrency, 
                throughput: throughput, 
                rampUpSeconds: rampUpSeconds, 
                holdForSeconds: holdForSeconds,
                iterations: iterations);

            await RunApp(config, plugin, testResultPublisher.Object);
            plugin.Calls.Should().BeLessThan(iterations);
            plugin.Calls.Should().BeGreaterThan(0);
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
        [InlineAutoMoqData(2, 2, 3, 2)]
        public async Task WhenThroughputIsSpecified_ThenRPSIsNotExceeded(
            int concurrency,
            double throughput,
            int rampUpSeconds,
            int holdForSeconds,
            Mock<IPlugin> plugin,
            Mock<ITestResultPublisher> testResultPublisher)
        {
            var config = GetConfig(
                concurrency: concurrency,
                throughput: throughput,
                rampUpSeconds: rampUpSeconds,
                holdForSeconds: holdForSeconds);

            await RunApp(config, plugin.Object, testResultPublisher.Object);

            var expectedTotal = throughput * holdForSeconds +
                (rampUpSeconds * throughput / 2);
            var tps = concurrency * throughput;

            plugin.Verify(n => n.ExecuteTestMethod(),
                Times.Between(Convert.ToInt32(expectedTotal - tps), Convert.ToInt32(expectedTotal + tps), Moq.Range.Inclusive));
        }

        [Theory]
        [InlineAutoMoqData(20, 0, 3, 25)]
        [InlineAutoMoqData(350, 2, 2, 80)]
        [InlineAutoMoqData(350, 0, 5, 80)]
        public async Task WhenMoreIterationsThanSingleThreadAllows_ThenThreadsAdapt(
            double throughput,
            int rampUpSeconds,
            int holdForSeconds,
            int iterations,
            Mock<ITestResultPublisher> testResultPublisher)
        {
            var plugin = new PluginFake();
            var config = GetConfig(
                throughput: throughput,
                rampUpSeconds: rampUpSeconds,
                holdForSeconds: holdForSeconds,
                iterations: iterations,
                openWorkload: true);

            await RunApp(config, plugin, testResultPublisher.Object);
            plugin.Calls.Should().Be(iterations);
            testResultPublisher.Verify(s => s.Publish(It.IsAny<TestResult>()), Times.Exactly(iterations));
        }

        [Theory]
        [InlineAutoMoqData(0, 1, 0, 4)]
        [InlineAutoMoqData(0, 0.8, 0, 4)]
        [InlineAutoMoqData(0, 2, 0, 3)]
        [InlineAutoMoqData(0, 20, 0, 4)]
        [InlineAutoMoqData(0, 1, 2, 4)]
        [InlineAutoMoqData(0, 0.8, 2, 5)]
        [InlineAutoMoqData(0, 2, 2, 2)]
        public async Task WhenOpenWorkload_ThenRPSIsNotExceeded(
            int concurrency,
            double throughput,
            int rampUpSeconds,
            int holdForSeconds,
            Mock<ITestResultPublisher> testResultPublisher)
        {
            var plugin = new PluginFake();
            var config = GetConfig(
                concurrency: concurrency, 
                throughput: throughput, 
                rampUpSeconds: rampUpSeconds, 
                holdForSeconds: holdForSeconds,
                openWorkload: true);

            await RunApp(config, plugin, testResultPublisher.Object);
            
            var expectedTotal = throughput * holdForSeconds +
              (rampUpSeconds * throughput / 2);

            plugin.Calls.Should().BeInRange(1, 
                Convert.ToInt32(expectedTotal + throughput));
        }

        [Theory]
        [InlineAutoMoqData(2,500,4)]
        public async Task WhenThinkTimeIsSpecified_ThenWaitIsObserved(
            int holdForSeconds,
            int thinkTimeMilliseconds,
            int expectedMax,
            Mock<IPlugin> plugin,
            Mock<ITestResultPublisher> testResultPublisher)
        {
            var config = GetConfig(holdForSeconds: holdForSeconds, thinkTimeMilliseconds: thinkTimeMilliseconds);
            await RunApp(config, plugin.Object, testResultPublisher.Object);
            plugin.Verify(n => n.ExecuteTestMethod(), Times.Between(1, expectedMax, Moq.Range.Inclusive));
        }

        [Theory]
        [InlineAutoMoqData(10, 2, 2)]
        [InlineAutoMoqData(8, 3, 2)]
        [InlineAutoMoqData(4, 0, 3)]
        public async Task WhenRampDownConcurrencyIsSpecified_ThenThreadsRpsDecreases(
            int concurrency,
            int holdForSeconds,
            int rampDownSeconds,
            Mock<IPlugin> plugin,
            Mock<ITestResultPublisher> testResultPublisher)
        {
            var thinkTime = 200;
            var config = GetConfig(
                concurrency: concurrency, 
                holdForSeconds: holdForSeconds, 
                rampDownSeconds: rampDownSeconds, 
                thinkTimeMilliseconds: thinkTime);
            await RunApp(config, plugin.Object, testResultPublisher.Object);

            var throughput = 6;
            var expected = throughput * concurrency * (holdForSeconds + 1) +
              (rampDownSeconds * concurrency * throughput / 2);

            plugin.Verify(n => n.ExecuteTestMethod(), Times.Between(0, expected, Moq.Range.Inclusive));
        }

        private async Task RunApp(
            Config config,
            IPlugin plugin,
            ITestResultPublisher testResultPublisher)
        {
            var cts = new CancellationTokenSource();
            var threadControl = new ThreadControl(config, plugin, testResultPublisher);
            var app = new Application(threadControl, testResultPublisher);

            cts.CancelAfter(config.TestDuration().Add(TimeSpan.FromMilliseconds(250)));
            await app.Run(cts.Token);
        }

        private Config GetConfig(
            int concurrency = 1,
            double throughput = 0,
            int rampUpSeconds = 0,
            int holdForSeconds = 1,
            int iterations = 0,
            bool openWorkload = false,
            int thinkTimeMilliseconds = 0,
            int rampDownSeconds = 0)
        {
            var config = new Config
            {
                Concurrency = new Concurrency
                {
                    Threads = concurrency,
                    RampUp = TimeSpan.FromSeconds(rampUpSeconds),
                    HoldFor = TimeSpan.FromSeconds(holdForSeconds),
                    RampDown = TimeSpan.FromSeconds(rampDownSeconds),
                    OpenWorkload = openWorkload
                },
                Throughput = new Throughput
                {
                    HoldFor = TimeSpan.FromSeconds(holdForSeconds),
                    RampUp = TimeSpan.FromSeconds(rampUpSeconds),
                    RampDown = TimeSpan.FromSeconds(rampDownSeconds),
                    Iterations = iterations,
                    Tps = throughput,
                    ThinkTime = TimeSpan.FromMilliseconds(thinkTimeMilliseconds)
                }
            };
            config.Throughput.Phases = config.BuildStandardThroughputPhases();
            config.Concurrency.Phases = config.BuildStandardConcurrencyPhases();
            return config;
        }

        class PluginFake : IPlugin
        {
            public int Calls;

            public async Task<object> ExecuteSetupMethod()
            {
                return await Task.FromResult(0);
            }

            public async Task<object> ExecuteTeardownMethod()
            {
                return await Task.FromResult(0);
            }

            public async Task<object> ExecuteTestMethod()
            {
                Thread.Sleep(400);
                await Task.Delay(1);
                return Interlocked.Increment(ref Calls);
            }
        }
    }
}

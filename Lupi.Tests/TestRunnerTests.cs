using Lupi.Configuration;
using Lupi.Core;
using Lupi.Services;
using System.Diagnostics;

namespace Lupi.Tests {
    public class TestRunnerTests 
    {
        [Theory]
        [AutoMoqData]
        public async Task WhenDurationSpecified_ThenDurationIsObserved(
            [Frozen]Config config,
            [Frozen]Mock<ITimeService> timeService,
            TestRunner testRunner)
        {
            var cts = new CancellationTokenSource();
            var timeout = TimeSpan.FromSeconds(10);
            var stopwatch = new Stopwatch();
            config.Concurrency.Phases = new List<ConcurrencyPhase>{
                new ConcurrencyPhase{
                    Duration = TimeSpan.FromSeconds(15),
                    Threads = 3
                }
            };

            var startTime = new DateTime(2020,05,01,13,0,0);
            var endTime = startTime.AddSeconds(60);
            timeService
                .SetupSequence((v) => v.Now())
                .Returns(startTime)
                .Returns(() => {
                    timeService.Setup(v => v.Now()).Returns(endTime);
                    return endTime;
                });

            stopwatch.Start();
            await testRunner.Run(cts.Token);
            stopwatch.Stop();
            stopwatch.ElapsedMilliseconds.Should().BeLessThan((long)timeout.TotalMilliseconds);
        }

        [Theory]
        [AutoMoqData]
        public async Task CheckIntervalWaitIsObserved(
            [Frozen]Config config,
            [Frozen]Mock<ITimeService> timeService,
            [Frozen]Mock<ISleepService> sleepService,
            TestRunner testRunner
        )
        {
            var cts = new CancellationTokenSource();
            var startTime = new DateTime(2020,05,01,13,0,0);
            var endTime = startTime.AddSeconds(60);
            config.Engine.CheckInterval = TimeSpan.FromSeconds(2);
            timeService
                .SetupSequence((v) => v.Now())
                .Returns(startTime)
                .Returns(() => {
                    timeService.Setup(v => v.Now()).Returns(endTime);
                    return endTime;
                });

            await testRunner.Run(cts.Token);
            sleepService.Verify(m => m.WaitFor(config.Engine.CheckInterval, cts.Token));
        }
    }
}

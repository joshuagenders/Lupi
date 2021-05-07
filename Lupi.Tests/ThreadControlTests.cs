using AutoFixture.Xunit2;
using FluentAssertions;
using Lupi.Configuration;
using Lupi.Core;
using Lupi.Services;
using Moq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Lupi.Tests {
    public class ThreadControlTests 
    {
        [Theory]
        [AutoMoqData]
        public async Task WhenDurationSpecified_ThenDurationIsObserved(
            [Frozen]Config config, 
            [Frozen]IThreadMarshall threadMarshall,
            [Frozen]ITokenManager tokenManager,
            [Frozen]Mock<ITimeService> timeService,
            ThreadControl threadControl)
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
            await threadControl.Run(cts.Token);
            stopwatch.Stop();
            stopwatch.ElapsedMilliseconds.Should().BeLessThan((long)timeout.TotalMilliseconds);
        }
        // WhenDurationSpecified_ThenDurationIsObserved
        // WhenMoreIterationsThanSingleThreadAllows_ThenThreadsAdapt
        // WhenThinkTimeIsSpecified_ThenWaitIsObserved
        // WhenRampDownConcurrencyIsSpecified_ThenThreadsRpsDecreases
    }
}
using System;
using System.Threading;
using AutoFixture.Xunit2;
using FluentAssertions;
using Lupi.Configuration;
using Lupi.Core;
using Xunit;

namespace Lupi.Tests 
{
    public class ThreadMarshallTests 
    {
        //todo
        // open workload
        // closed workload
        [Theory]
        [AutoMoqData]
        public void WhenRampUpSpecified_ThenThreadsIncrease(
            [Frozen]ITestThreadFactory testThreadFactory,
            [Frozen]ITokenManager tokenManager,
            [Frozen]Config config,
            ThreadMarshall threadMarshall)
        {
            var cts = new CancellationTokenSource();
            var startTime = new DateTime(2020,05,02, 13,0,0);
            var endTime = startTime.AddMinutes(5);
            config.Concurrency.RampUp = TimeSpan.FromMinutes(1);
            config.Concurrency.Threads = 20;
            config.Concurrency.HoldFor = TimeSpan.FromMinutes(4);
            config.Concurrency.OpenWorkload = false;
            config.Concurrency.Phases = config.BuildStandardConcurrencyPhases();
            threadMarshall.AdjustThreadLevels(startTime, startTime.AddSeconds(30), cts.Token);
            threadMarshall.GetThreadCount().Should().BeCloseTo(config.Concurrency.Threads / 2, 1);
        }

        [Theory]
        [AutoMoqData]
        public void WhenRampDownSpecified_ThenThreadsDecrease()
        {
            throw new NotImplementedException();
        }

        [Theory]
        [AutoMoqData]
        public void WhenConcurrencySpecified_ThenThreadLevelIsSet()
        {
            throw new NotImplementedException();
        }
    }
}
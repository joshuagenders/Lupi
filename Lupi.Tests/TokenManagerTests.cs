using System;
using System.Collections.Generic;
using AutoFixture.Xunit2;
using FluentAssertions;
using Lupi.Configuration;
using Lupi.Core;
using Xunit;

namespace Lupi.Tests 
{
    public class TokenManagerTests 
    {
        [Theory]
        [InlineAutoMoqData(1)]
        [InlineAutoMoqData(100)]
        public void WhenIterationsSpecified_ThenIterationsAreNotExceeded(
            int iterations,
            [Frozen]Config config,
            TokenManager tokenManager)
        {
            config.Throughput.Iterations = iterations;
            config.Throughput.HoldFor = TimeSpan.FromSeconds(60);
            config.Throughput.Tps = iterations;
            config.Throughput.Phases = new List<Phase>()
            {
                new Phase 
                {
                    Tps = 30,
                    Duration = TimeSpan.FromSeconds(60)
                }
            };
            var startTime = new DateTime(2020, 05, 05, 13, 0, 0);
            var endTime = startTime.AddSeconds(60);
            tokenManager.Initialise(startTime, endTime);
            tokenManager.ReleaseTokens(startTime);
            tokenManager.ReleaseTokens(endTime.AddSeconds(-1));
            tokenManager.GetTokenCount().Should().Be(iterations);
        } 

        [Theory]
        [AutoMoqData]
        public void WhenDurationSpecified_ThenDurationIsObserved(
            [Frozen]Config config,
            TokenManager tokenManager)
        {
            config.Throughput.HoldFor = TimeSpan.FromSeconds(60);
            config.Throughput.Tps = 20;
            config.Throughput.Phases = new List<Phase>()
            {
                new Phase 
                {
                    Tps = 30,
                    Duration = TimeSpan.FromSeconds(60)
                }
            };
            var startTime = new DateTime(2020, 05, 05, 13, 0, 0);
            var endTime = startTime.AddSeconds(60);
            tokenManager.Initialise(startTime, endTime);
            tokenManager.ReleaseTokens(startTime);
            tokenManager.ReleaseTokens(endTime.AddSeconds(-1));
            tokenManager.ReleaseTokens(endTime);
            var endTokenCount = tokenManager.GetTokenCount();
            tokenManager.ReleaseTokens(endTime.AddMinutes(10));
            tokenManager.GetTokenCount().Should().Be(endTokenCount);
        }

        [Theory]
        [InlineAutoMoqData(1, 0, 10, 0, 10)]
        [InlineAutoMoqData(1, 10, 10, 0, 15)]
        [InlineAutoMoqData(1, 0, 10, 10, 15)]
        [InlineAutoMoqData(1, 10, 20, 10, 30)]
        [InlineAutoMoqData(1, 0, 0, 0, 0)]
        [InlineAutoMoqData(0, 10, 10, 10, 0)]

        public void WhenThroughputIsSpecified_ThenRPSIsNotExceeded(
            int tps,
            int rampUpSeconds,
            int holdForSeconds,
            int rampDownSeconds,
            int expectedTotal,
            [Frozen]Config config,
            TokenManager tokenManager)
        {
            config.Concurrency.OpenWorkload = true;
            config.Throughput.Tps = tps;
            config.Throughput.RampUp = TimeSpan.FromSeconds(rampUpSeconds);
            config.Throughput.HoldFor = TimeSpan.FromSeconds(holdForSeconds);
            config.Throughput.RampDown = TimeSpan.FromSeconds(rampDownSeconds);

            config.Throughput.Phases = config.BuildStandardThroughputPhases();

            var startTime = new DateTime(2020, 05, 05, 13, 0, 0);
            var endTime = startTime.AddSeconds(rampUpSeconds + holdForSeconds + rampDownSeconds);
            tokenManager.Initialise(startTime, endTime);
            tokenManager.ReleaseTokens(startTime);
            if (rampUpSeconds > 0)
                tokenManager.ReleaseTokens(startTime.AddSeconds(rampUpSeconds));

            if (holdForSeconds > 0)
                tokenManager.ReleaseTokens(startTime.AddSeconds(rampUpSeconds + holdForSeconds));
    
            tokenManager.ReleaseTokens(endTime);
            var actualTotal = tokenManager.GetTokenCount();
            actualTotal.Should().Be(expectedTotal);
        }
    }
}
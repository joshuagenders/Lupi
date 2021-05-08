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
            tokenManager.Initialise(startTime, startTime.AddSeconds(60));
            tokenManager.ReleaseTokens(startTime);
            tokenManager.ReleaseTokens(endTime.AddSeconds(-1));
            tokenManager.GetTokenCount().Should().Be(iterations);
        } 

        [Fact]
        public void WhenDurationSpecified_ThenDurationIsObserved()
        {
            //no tokens released after end time
            throw new NotImplementedException();
        }

        [Fact]
        public void WhenThroughputIsSpecified_ThenRPSIsNotExceeded()
        {
            // correct token count for periods
            throw new NotImplementedException();
        }
 
        [Fact]
        public void WhenMoreIterationsThanDurationAllows_ThenTestExitsEarly()
        {
            throw new NotImplementedException();
        } 

        [Fact]
        public void WhenRampDownConcurrencyIsSpecified_ThenTokenCountDecreases()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public void WhenRampUpConcurrencyIsSpecified_ThenTokenCountIncreases()
        {
            throw new NotImplementedException();
        }
    }
}
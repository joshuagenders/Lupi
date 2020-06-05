using FluentAssertions;
using Lupi.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Lupi.Tests
{
    public class ConfigExtensionTests
    {
        //todo
        [Theory]
        [InlineAutoMoqData(1, 0, 1, 0, 0, 500, 0.5)]
        [InlineAutoMoqData(1, 0, 1, 0, 0, 999, 0.999)]
        [InlineAutoMoqData(1, 0, 2, 0, 0, 1000, 1)]
        [InlineAutoMoqData(2, 0, 2, 0, 0, 500, 1)]
        [InlineAutoMoqData(5, 0, 2, 0, 0, 500, 2.5)]
        [InlineAutoMoqData(10, 20, 0, 0, 0, 1000, 0.25)]
        [InlineAutoMoqData(10, 20, 1, 0, 0, 20000, 100.24)]
        public void ThroughputForStandardPhasesIsCorrect(
            int throughput,
            int rampUpSeconds,
            int holdForSeconds,
            int rampDownSeconds,
            int startMs,
            int endMs,
            double tokenCount)
        {
            var config = new Config
            {
                Throughput = new Throughput
                {
                    HoldFor = TimeSpan.FromSeconds(holdForSeconds),
                    RampUp = TimeSpan.FromSeconds(rampUpSeconds),
                    RampDown = TimeSpan.FromSeconds(rampDownSeconds),
                    Tps = throughput
                }

            };
            var phases = config.BuildStandardThroughputPhases();
            var start = DateTime.UtcNow;
            var result = phases.GetTokensForPeriod(start, start.AddMilliseconds(startMs), start.AddMilliseconds(endMs));
            result.Should().Be(tokenCount);
        }
    }
}

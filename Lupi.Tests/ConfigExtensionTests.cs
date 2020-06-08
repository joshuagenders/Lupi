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
        [InlineAutoMoqData(10, 20, 1, 0, 0, 20000, 100)]
        [InlineAutoMoqData(1, 1, 0, 0, 0, 1000, 0.5)]
        [InlineAutoMoqData(1, 0, 0, 1, 0, 1000, 0.5)]
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

        [Theory]
        [InlineAutoMoqData(0, 5, 10, 1, 0, 1000, 7.5)]
        [InlineAutoMoqData(0, 0, 10, 1, 0, 1000, 5)]
        [InlineAutoMoqData(20, 0, 0, 1, 0, 1000, 20)]
        public void ThroughputForCustomPhasesIsCorrect(
            int tps,
            int fromTps,
            int toTps,
            int durationSeconds,
            int startMs,
            int endMs,
            double tokenCount)
        {
            var phases = new List<Phase>
            {
                new Phase
                {
                    Duration = TimeSpan.FromMilliseconds(543),
                    Tps = 4
                },
                new Phase
                {
                    Duration = TimeSpan.FromSeconds(durationSeconds),
                    FromTps = fromTps,
                    ToTps = toTps,
                    Tps = tps
                },
                new Phase
                {
                    Duration = TimeSpan.FromSeconds(3),
                    Tps = 5
                },
            };
            var start = DateTime.UtcNow;
            var result = phases.GetTokensForPeriod(start, start.AddMilliseconds(startMs + 543), start.AddMilliseconds(endMs + 543));
            result.Should().Be(tokenCount);
        }
    }
}

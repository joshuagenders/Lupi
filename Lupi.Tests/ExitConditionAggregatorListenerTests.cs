using Lupi.Configuration;
using Lupi.Listeners;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Lupi.Tests
{
    public class ExitConditionAggregatorListenerTests
    {
        [Theory]
        [InlineAutoMoqData("passed if Min > 0 for 1 periods", 1)]
        [InlineAutoMoqData("passed if PeriodAverage < 200 for 5 periods", 6)]
        [InlineAutoMoqData("failed if PeriodMax > 25 for 1 seconds", 20)]
        public async Task WhenConditionMet_ThenTestExits(string condition, int periods,
            Mock<IExitSignal> exitSignal)
        {
            var exitCondition = YamlHelper.Deserialize<ExitCondition>(condition);
            var config = new Config
            {
                ExitConditions = new List<ExitCondition>
                {
                    exitCondition
                },
                Engine = new Engine
                {
                    AggregationInterval = TimeSpan.FromMilliseconds(250)
                }
            };
            var exitConditionAggregatorListener = new ExitConditionAggregatorListener(config, exitSignal.Object);
            for (var i = 0; i < periods; i++)
            {
                await exitConditionAggregatorListener.OnResult(new AggregatedResult
                {
                    Min = 1,
                    PeriodAverage = 150,
                    PeriodMax = 160
                }, default);
            }
            exitSignal.Verify(e => e.Signal(It.IsAny<string>(), exitCondition.PassedFailed.Equals("passed")));
        }

        [Theory]
        [InlineAutoMoqData("passed if Min = 0 for 1 periods", 1)]
        [InlineAutoMoqData("passed if PeriodAverage > 200 for 5 periods", 6)]
        [InlineAutoMoqData("failed if PeriodMax > 25 for 20 seconds", 20)]
        public async Task WhenConditionNotMet_ThenTestDoesntExit(string condition, int periods,
            Mock<IExitSignal> exitSignal)
        {
            var exitCondition = YamlHelper.Deserialize<ExitCondition>(condition);
            var config = new Config
            {
                ExitConditions = new List<ExitCondition>
                {
                    exitCondition
                },
                Engine = new Engine
                {
                    AggregationInterval = TimeSpan.FromMilliseconds(250)
                }
            };
            var exitConditionAggregatorListener = new ExitConditionAggregatorListener(config, exitSignal.Object);
            for (var i = 0; i < periods; i++)
            {
                await exitConditionAggregatorListener.OnResult(new AggregatedResult
                {
                    Min = 1,
                    PeriodAverage = 150,
                    PeriodMax = 160
                }, default);
            }
            exitSignal.Verify(e => e.Signal(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        }
    }
}

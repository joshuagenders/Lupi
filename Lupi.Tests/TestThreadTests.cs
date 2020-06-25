using FluentAssertions;
using System;
using Xunit;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using System.Diagnostics;
using AutoFixture.Xunit2;

namespace Lupi.Tests
{
    public class TestThreadTests
    {
        [Theory]
        [InlineAutoMoqData()]
        public async Task WhenThreadControlSignals_ThenThreadStops(
            [Frozen]Mock<IThreadControl> threadControl,
            TestThread testThread)
        {
            var cts = new CancellationTokenSource();
            var longTime = 100000;
            cts.CancelAfter(longTime);
            threadControl
                .Setup(t => t.RequestTaskExecution(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var watch = new Stopwatch();
            watch.Start();
            await testThread.Run(DateTime.UtcNow, cts.Token);
            watch.Stop();
            watch.ElapsedMilliseconds.Should().BeLessThan(longTime);
        }
    }
}

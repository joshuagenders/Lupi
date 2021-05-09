using FluentAssertions;
using System;
using Xunit;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using System.Diagnostics;
using AutoFixture.Xunit2;
using Lupi.Core;
using Lupi.Results;
using Lupi.Configuration;

namespace Lupi.Tests
{
    public class TestThreadTests
    {
        [Theory]
        [InlineAutoMoqData()]
        public async Task WhenThreadControlSignals_ThenThreadStops(
            [Frozen]Mock<ITokenManager> tokenManager,
            TestThread testThread)
        {
            var cts = new CancellationTokenSource();
            var longTime = 100000;
            cts.CancelAfter(longTime);
            tokenManager
                .Setup(t => t.RequestTaskExecution(It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var watch = new Stopwatch();
            watch.Start();
            await testThread.Run(cts.Token);
            watch.Stop();
            watch.ElapsedMilliseconds.Should().BeLessThan(longTime - 100);
        }

        [Theory]
        [AutoMoqData]
        public async Task WhenExceptionsReturned_ThenTestsAreFailed(
            [Frozen]Mock<IPlugin> plugin,
            [Frozen]Mock<ITestResultPublisher> testResultPublisher,
            [Frozen]Mock<ITokenManager> tokenManager,
            [Frozen]Config config,
            TestThread testThread)
        {
            var cts = new CancellationTokenSource();
            cts.CancelAfter(5000);
            config.Concurrency.OpenWorkload = false;
            plugin.Setup(p => p.ExecuteTestMethod()).Throws(new Exception());
            tokenManager
                .SetupSequence(t => t.RequestTaskExecution(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(() => {
                    tokenManager.Setup(t => t.RequestTaskExecution(It.IsAny<CancellationToken>()))
                                .ReturnsAsync(false);
                    return false;
                });
            await testThread.Run(cts.Token);
            testResultPublisher.Verify(r => r.Publish(It.Is<TestResult>(t => t.Passed == false)));
        }

        [Fact]
        public void WhenTaskReturnsFalse_ThenTestsAreFailed()
        {
            throw new NotImplementedException();
        }
    }
}

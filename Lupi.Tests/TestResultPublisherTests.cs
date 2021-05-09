using System;
using System.Threading;
using System.Threading.Tasks;
using Lupi.Listeners;
using Lupi.Results;
using Moq;
using Xunit;

namespace Lupi.Tests
{
    public class TestResultPublisherTests
    {
        [Theory]
        [AutoMoqData]
        public async Task CannotSubscribeTwice(
            Mock<ITestResultListener> testResultListener,
            TestResultPublisher testResultPublisher,
            CancellationTokenSource cts)
        {
            cts.CancelAfter(5000);
            testResultPublisher.Subscribe(testResultListener.Object);
            testResultPublisher.Subscribe(testResultListener.Object);
            testResultPublisher.Publish(new TestResult());
            await testResultPublisher.Process(cts.Token);
            testResultListener.Verify(r => r.OnResult(It.IsAny<TestResult[]>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [AutoMoqData]
        public async Task ProcessesAllResultsBeforeExiting(
            Mock<ITestResultListener> testResultListener,
            TestResultPublisher testResultPublisher,
            CancellationTokenSource cts)
        {
            testResultPublisher.Subscribe(testResultListener.Object);
            testResultPublisher.Publish(new TestResult());
            cts.Cancel();
            await testResultPublisher.Process(cts.Token);
            testResultListener.Verify(r => r.OnResult(It.IsAny<TestResult[]>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
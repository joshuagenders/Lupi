using Lupi.Core;
using Lupi.Listeners;
using Lupi.Results;

namespace Lupi.Tests
{
    public class ApplicationTests 
    {
        [Theory]
        [AutoMoqData]
        public async Task WhenExitSignalled_ThenApplicationCompletes(
            [Frozen]Mock<ITestResultPublisher> testResultPublisher,
            [Frozen]Mock<ISystemMetricsPublisher> systemMetricsPublisher,
            [Frozen]Mock<IAggregator> aggregator,
            Application application)
        {
            await application.Run();
            testResultPublisher.VerifySet(t => t.TestCompleted = true);
            testResultPublisher.Verify(a => a.Process(It.IsAny<CancellationToken>()), Times.Once);
            systemMetricsPublisher.VerifySet(t => t.TestCompleted = true);
            systemMetricsPublisher.Verify(a => a.Process(It.IsAny<CancellationToken>()), Times.Once);
            aggregator.Verify(a => a.Process(It.IsAny<CancellationToken>()), Times.Once);
            aggregator.VerifySet(t => t.TestCompleted = true);
        }
    }
}
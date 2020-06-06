using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Lupi
{
    public class Application : IApplication
    {
        private readonly IThreadControl _threadControl;
        private readonly ITestResultPublisher _testResultPublisher;

        public Application(
            IThreadControl threadControl,
            ITestResultPublisher testResultPublisher)
        {
            _threadControl = threadControl;
            _testResultPublisher = testResultPublisher;
        }

        public async Task Run(CancellationToken ct)
        {
            try
            {
                var startTime = DateTime.UtcNow;
                var reportTask = Task.Run(() => _testResultPublisher.Process(ct), ct);

                DebugHelper.Write($"Starting tests. Start Time {startTime}");
                await _threadControl.Run(startTime, ct);
                _testResultPublisher.TestCompleted = true;
                DebugHelper.Write($"Tests completed. Awaiting reporting task.");
                await reportTask;
                DebugHelper.Write($"Reporting completed.");
            }
            catch (TaskCanceledException) { }
            catch (OperationCanceledException) { }
            catch (AggregateException e) when (e.InnerExceptions.All(x => x is TaskCanceledException || x is OperationCanceledException)) { }
        }
    }

    public interface IApplication
    {
        Task Run(CancellationToken ct);
    }
}

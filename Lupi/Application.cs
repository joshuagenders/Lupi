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
        private readonly SemaphoreSlim _executionSemaphore;

        public Application(
            IThreadControl threadControl,
            ITestResultPublisher testResultPublisher)
        {
            _threadControl = threadControl;
            _testResultPublisher = testResultPublisher;
            _executionSemaphore = new SemaphoreSlim(1);
        }

        public async Task Run(CancellationToken ct)
        {
            await _executionSemaphore.WaitAsync(ct);
            DebugHelper.Write("==== run ====");
            try
            {
                var startTime = DateTime.UtcNow;
                var reportTask = Task.Run(() => _testResultPublisher.Process(ct), ct);
                await _threadControl.Run(startTime, ct);
                _testResultPublisher.TestCompleted = true;
                await reportTask;
            }
            catch (TaskCanceledException) { }
            catch (OperationCanceledException) { }
            catch (AggregateException e) when (e.InnerExceptions.All(x => x is TaskCanceledException || x is OperationCanceledException)) { }
            finally
            {
                _executionSemaphore.Release();
            }
        }
    }

    public interface IApplication
    {
        Task Run(CancellationToken ct);
    }
}

using Lupi.Listeners;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Lupi
{
    public class Application : IApplication
    {
        private readonly IThreadControl _threadControl;
        private readonly ITestResultPublisher _testResultPublisher;
        private readonly IAggregator _aggregator;

        public Application(
            IThreadControl threadControl,
            ITestResultPublisher testResultPublisher,
            IAggregator aggregator)
        {
            _threadControl = threadControl;
            _testResultPublisher = testResultPublisher;
            _aggregator = aggregator;
        }

        public async Task Run(CancellationToken ct)
        {
            try
            {
                var startTime = DateTime.UtcNow;
                var tasks = new List<Task> {
                    Task.Run(() => _testResultPublisher.Process(ct), ct),
                    Task.Run(() => _aggregator.Process(ct), ct)
                };
                DebugHelper.Write($"Starting tests. Start Time {startTime}");
                await _threadControl.Run(startTime, ct);
                _testResultPublisher.TestCompleted = true;
                _aggregator.TestCompleted = true;
                DebugHelper.Write($"Tests completed. Awaiting reporting tasks.");
                await Task.WhenAll(tasks);
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

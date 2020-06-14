using Lupi.Listeners;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger _logger;

        public Application(
            IThreadControl threadControl,
            ITestResultPublisher testResultPublisher,
            IAggregator aggregator,
            ILogger logger)
        {
            _threadControl = threadControl;
            _testResultPublisher = testResultPublisher;
            _aggregator = aggregator;
            _logger = logger;
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
                _logger.LogInformation("Starting tests. Start time: {startTime}", startTime);
                await _threadControl.Run(startTime, ct);
                _testResultPublisher.TestCompleted = true;
                _aggregator.TestCompleted = true;
                _logger.LogInformation($"Tests completed. Awaiting reporting tasks");
                await Task.WhenAll(tasks);
                _logger.LogInformation($"Reporting complete. Run Complete.");
            }
            catch (TaskCanceledException ex) 
            {
                _logger.LogError("A task was cancelled.", ex);
            }
            catch (OperationCanceledException ex) 
            {
                _logger.LogError("An operation was cancelled.", ex);
            }
            catch (AggregateException ex) when (ex.InnerExceptions.All(x => x is TaskCanceledException || x is OperationCanceledException)) 
            {
                _logger.LogError("An aggregate exception occured where all inner exceptions are Task or Operation cancellations.", ex);
            }
        }
    }

    public interface IApplication
    {
        Task Run(CancellationToken ct);
    }
}

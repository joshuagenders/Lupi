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
        private readonly IExitSignal _exitSignal;
        private readonly ISystemMetricsPublisher _systemMetricsPublisher;
        private readonly ILogger<IApplication> _logger;

        public Application(
            IThreadControl threadControl,
            ITestResultPublisher testResultPublisher,
            IAggregator aggregator,
            IExitSignal exitSignal,
            ISystemMetricsPublisher systemMetricsPublisher,
            ILogger<IApplication> logger)
        {
            _threadControl = threadControl;
            _testResultPublisher = testResultPublisher;
            _aggregator = aggregator;
            _exitSignal = exitSignal;
            _systemMetricsPublisher = systemMetricsPublisher;
            _logger = logger;
        }

        public async Task<int> Run()
        {
            using var cts = new CancellationTokenSource();
            var runTask = RunTest(cts.Token);
            var exitTask = _exitSignal.AwaitForSignal(cts.Token);
            await Task.WhenAny(runTask, exitTask);
            if (_exitSignal.Signalled)
            {
                cts.Cancel();
                Console.WriteLine(_exitSignal.SignalReason);
                _testResultPublisher.TestCompleted = true;
                return _exitSignal.Passed ? 0 : 1;
            }
            return await runTask;
        }

        private async Task<int> RunTest(CancellationToken ct)
        {
            try
            {
                var startTime = DateTime.UtcNow;
                var tasks = new List<Task> {
                    Task.Run(() => _testResultPublisher.Process(ct), ct),
                    Task.Run(() => _systemMetricsPublisher.Run(ct), ct),
                    Task.Run(() => _aggregator.Process(ct), ct)
                };
                _logger.LogInformation("Starting tests. Start time: {startTime}", startTime);
                await _threadControl.Run(startTime, ct);
                _testResultPublisher.TestCompleted = true;
                _systemMetricsPublisher.TestCompleted = true;
                _aggregator.TestCompleted = true;
                _logger.LogInformation($"Tests completed. Awaiting reporting tasks");
                await Task.WhenAll(tasks);
                _logger.LogInformation($"Reporting complete. Run Complete.");
                return 0;
            }
            catch (TaskCanceledException ex) 
            {
                _logger.LogError("A task was cancelled.", ex);
                return 1;
            }
            catch (OperationCanceledException ex) 
            {
                _logger.LogError("An operation was cancelled.", ex);
                return 1;
            }
            catch (AggregateException ex) when (ex.InnerExceptions.All(x => x is TaskCanceledException || x is OperationCanceledException)) 
            {
                _logger.LogError("An aggregate exception occured where all inner exceptions are Task or Operation cancellations.", ex);
                return 1;
            }
        }
    }

    public interface IApplication
    {
        Task<int> Run();
    }
}

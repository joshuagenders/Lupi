using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Yakka
{
    public class Application : IApplication
    {
        private readonly IThreadControl _threadControl;
        private readonly IThreadAllocator _threadAllocator;
        private readonly Config _config;
        private readonly SemaphoreSlim _executionSemaphore;

        public Application(
            IThreadAllocator threadAllocator,
            IThreadControl threadControl,
            Config config)
        {
            _threadControl = threadControl;
            _threadAllocator = threadAllocator;
            _config = config;
            _executionSemaphore = new SemaphoreSlim(1);
        }

        public async Task Run(CancellationToken ct)
        {
            await _executionSemaphore.WaitAsync(ct);
            try
            {
                var startTime = DateTime.UtcNow;
                var testDuration = _config.TestDuration();
                var threadCreationTask = Task.Run(() =>
                    _threadAllocator.StartThreads(
                        startTime, 
                        _config.Concurrency.Threads, 
                        Convert.ToInt32(_config.Concurrency.RampUp.TotalSeconds), ct), ct);
                var testDurationTask = _config.ThroughputEnabled
                    ? Task.Run(() => _threadControl.ReleaseTokens(startTime, ct), ct)
                    : Task.Run(() => Task.Delay(testDuration, ct), ct);

                await threadCreationTask;
                await testDurationTask;
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

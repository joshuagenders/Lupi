using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Yakka
{
    public class Application : IApplication
    {
        private readonly IThreadControl _threadControl;
        private readonly Config _config;
        private readonly SemaphoreSlim _executionSemaphore;

        public Application(
            IThreadControl threadControl,
            Config config)
        {
            _threadControl = threadControl;
            _config = config;
            _executionSemaphore = new SemaphoreSlim(1);
        }

        public async Task Run(CancellationToken ct)
        {
            await _executionSemaphore.WaitAsync(ct);
            DebugHelper.Write("==== run ====");
            try
            {
                var startTime = DateTime.UtcNow;
                var testDuration = _config.TestDuration();
                var threadCreationTask = Task.Run(() =>
                    _threadControl.AllocateThreads(startTime, ct), ct);
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

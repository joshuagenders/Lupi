using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Yakka
{
    public class Application : IApplication
    {
        private readonly IThreadControl _threadControl;
        private readonly SemaphoreSlim _executionSemaphore;

        public Application(
            IThreadControl threadControl)
        {
            _threadControl = threadControl;
            _executionSemaphore = new SemaphoreSlim(1);
        }

        public async Task Run(CancellationToken ct)
        {
            await _executionSemaphore.WaitAsync(ct);
            DebugHelper.Write("==== run ====");
            try
            {
                var startTime = DateTime.UtcNow;
                var tasks = new Task[] {
                    Task.Run(() => _threadControl.ReleaseTokens(startTime, ct), ct),
                    Task.Run(() => _threadControl.AllocateThreads(startTime, ct), ct),
                };
                await Task.WhenAll(tasks);
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

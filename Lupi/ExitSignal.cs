using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lupi
{
    public interface IExitSignal
    {
        void Signal(string signalReason);
        Task AwaitForSignal(TimeSpan timeout, CancellationToken ct);
        Task AwaitForSignal(CancellationToken ct);
        string SignalReason { get; }
        bool Signalled { get; }
    }

    public class ExitSignal : IExitSignal
    {
        private SemaphoreSlim _semaphore = new SemaphoreSlim(0);
        public bool Signalled { get; internal set; }
        public string SignalReason { get; internal set; }
        
        public void Signal(string signalReason)
        {
            SignalReason = signalReason;
            Signalled = true;
            _semaphore.Release();
        }

        public async Task AwaitForSignal(TimeSpan timeout, CancellationToken ct)
        {
            await _semaphore.WaitAsync(timeout, ct);
        }

        public async Task AwaitForSignal(CancellationToken ct)
        {
            await _semaphore.WaitAsync(ct);
        }
    }
}

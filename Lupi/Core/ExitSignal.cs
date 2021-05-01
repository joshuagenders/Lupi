using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lupi.Core
{
    public interface IExitSignal
    {
        void Signal(string signalReason, bool passed);
        Task AwaitForSignal(TimeSpan timeout, CancellationToken ct);
        Task AwaitForSignal(CancellationToken ct);
        string SignalReason { get; }
        bool Passed { get; }
        bool Signalled { get; }
    }

    public class ExitSignal : IExitSignal
    {
        private SemaphoreSlim _semaphore = new SemaphoreSlim(0);
        public bool Signalled { get; internal set; }
        public string SignalReason { get; internal set; }
        public bool Passed  { get; internal set; }

        public void Signal(string signalReason, bool passed)
        {
            SignalReason = signalReason;
            Signalled = true;
            Passed = passed;
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

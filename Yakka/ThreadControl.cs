using System;
using System.Threading;
using System.Threading.Tasks;

namespace Yakka
{
    public class ThreadControl : IThreadControl
    {
        private readonly SemaphoreSlim _taskExecution;
        private readonly SemaphoreSlim _taskIncrement;
        private readonly Config _config;

        private bool _enabled { get; set; }
        public double _throughput { get; }
        public int _iterations { get; }
        public int _rampUpSeconds { get; }
        public int _holdForSeconds { get; }

        private int _executionRequestCount;

        public ThreadControl(Config config)
        {
            _taskExecution = new SemaphoreSlim(0);
            _taskIncrement = new SemaphoreSlim(1);
            _config = config;
            _throughput = config.Throughput.Tps;
            _iterations = config.Throughput.Iterations;
            _rampUpSeconds = Convert.ToInt32(config.Concurrency.RampUp.TotalSeconds);
            _holdForSeconds = Convert.ToInt32(config.Throughput.HoldFor.TotalSeconds);
            _enabled = config.ThroughputEnabled;
            _executionRequestCount = 0;
        }

        public async Task<bool> RequestTaskExecution(DateTime startTime, CancellationToken ct)
        {
            DebugHelper.Write($"task execution request start");
            if (_enabled)
            {
                DebugHelper.Write($"task execution enabled, waiting");
                await _taskExecution.WaitAsync(ct);
                DebugHelper.Write($"waiting complete");
            }
            int iterations;
            try
            {
                await _taskIncrement.WaitAsync(ct);
                iterations = Interlocked.Increment(ref _executionRequestCount);
            }
            finally
            {
                if (_taskIncrement.CurrentCount <= 0)
                {
                    _taskIncrement.Release();
                }
            }
            DebugHelper.Write($"iterations {iterations}");
            var isCompleted = IsTestComplete(EndTime(startTime, _rampUpSeconds, _holdForSeconds), iterations);
            DebugHelper.Write($"is test completed {isCompleted}");
            return isCompleted;
        }

        // private int TotalAllowedRequestsToNow(int millisecondsEllapsed)
        // {
        //     if (millisecondsEllapsed <= 0 || _throughput <= 0) { 
        //         return 0;
        //     }
        //     double totalRpsToNow;
        //     var secondsEllapsed = Convert.ToInt32(millisecondsEllapsed / 1000);
        //     DebugHelper.Write($"calculating tokens, seconds ellapsed {secondsEllapsed}");
        //     if (_rampUpSeconds > 0)
        //     {
        //         if (secondsEllapsed > _rampUpSeconds)
        //         {
        //             totalRpsToNow = (_throughput * _rampUpSeconds / 2)
        //                 + (secondsEllapsed - _rampUpSeconds) * _throughput;
        //         }
        //         else
        //         {
        //             totalRpsToNow = (_throughput * millisecondsEllapsed) / 1000 / 2;
        //         }
        //     }
        //     else
        //     {
        //         totalRpsToNow = (_throughput * millisecondsEllapsed) / 1000;
        //     }
        //     DebugHelper.Write($"total allowed requests to now {totalRpsToNow}");
        //     return Convert.ToInt32(totalRpsToNow);
        // }

        public async Task ReleaseTokens(DateTime startTime, CancellationToken ct)
        {
            if (!_enabled)
            {
                return;
            }
            var tokensReleased = 0;
            var endTime = EndTime(startTime, _rampUpSeconds, _holdForSeconds);
            while (!IsTestComplete(endTime, tokensReleased) && !ct.IsCancellationRequested)
            {
                var millisecondsEllapsed = Convert.ToInt32(DateTime.UtcNow.Subtract(startTime).TotalMilliseconds);
                var tokensToNow = _config.Throughput.Phases.TotalAllowedRequestsToNow(millisecondsEllapsed);

                if (tokensToNow > tokensReleased)
                {
                    var tokensToRelease = Convert.ToInt32(tokensToNow - tokensReleased);
                    if (_iterations > 0)
                    {
                        if (int.MaxValue - tokensToRelease < tokensReleased)
                        {
                            tokensToRelease = int.MaxValue - tokensReleased;
                        }
                        if (tokensToRelease + tokensReleased > _iterations)
                        {
                            tokensToRelease = _iterations - tokensReleased;
                        }
                    }
                    if (tokensToRelease > 0)
                    {
                        DebugHelper.Write($"releasing {tokensToRelease}");
                        tokensReleased += tokensToRelease;
                        DebugHelper.Write($"total tokens released {tokensReleased}");
                        _taskExecution.Release(tokensToRelease);
                    }
                }
                await Task.Delay(TimeSpan.FromMilliseconds(100), ct); //todo configure
            }
        }

        private bool IsTestComplete(DateTime endTime, int iterations) => 
            DateTime.UtcNow >= endTime || IterationsExceeded(iterations);

        private bool IterationsExceeded(int iterations) =>
            _iterations > 0 && iterations > _iterations;

        private DateTime EndTime(DateTime startTime, int rampUpSeconds, int holdForSeconds) =>
            startTime.AddSeconds(rampUpSeconds + holdForSeconds);
    }

    public interface IThreadControl
    {
        Task<bool> RequestTaskExecution(DateTime startTime, CancellationToken ct);
        Task ReleaseTokens(DateTime startTime, CancellationToken ct);
    }
}

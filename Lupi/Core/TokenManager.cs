using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using JustEat.StatsD;
using Lupi.Configuration;
using Lupi.Services;
using Microsoft.Extensions.Logging;

namespace Lupi.Core 
{

    public interface ITokenManager 
    {
        void Initialise(DateTime startTime, DateTime endTime);
        int GetTokenCount();
        void ReleaseTokens(DateTime now);
        Task<bool> RequestTaskExecution(CancellationToken ct);
        void RequestTaskDiscontinues();
    }

    public class TokenManager : ITokenManager
    {
        private readonly Config _config;
        private readonly ITimeService _timeService;
        private readonly ILogger<ITokenManager> _logger;
        private readonly IStatsDPublisher _stats;
        private readonly ConcurrentQueue<bool> _taskKill;

        private double _partialTokens;
        private int _iterationsRemaining;
        private SemaphoreSlim _taskExecution;
        private SemaphoreSlim _taskDecrement;
        
        private DateTime _startTime;
        private DateTime _endTime;
        private DateTime _lastTime;


        public TokenManager (Config config, ITimeService timeService, ILogger<ITokenManager> logger)
        {
            _config = config;
            _timeService = timeService;
            _logger = logger;
            _taskKill = new ConcurrentQueue<bool>();
        }

        public int GetTokenCount()
        {
            return _taskExecution.CurrentCount;
        }

        public void Initialise(DateTime startTime, DateTime endTime)
        {
            _taskExecution = new SemaphoreSlim(0);
            _taskDecrement = new SemaphoreSlim(1); // used to lock decrement of iterations to prevent race conditions
            _partialTokens = 0;
            _startTime = startTime;
            _lastTime = startTime;
            _endTime = endTime;
            _iterationsRemaining = _config.Throughput.Iterations > 0
                ? _config.Throughput.Iterations
                : 0;
            _taskKill.Clear();
        }

        public void ReleaseTokens(DateTime now)
        {
            if (_taskExecution == (default))
            {
                _logger.LogInformation("ReleaseTokens invoked before initialise");
                return;
            }
            if (_config.Throughput.Iterations > 0 && _iterationsRemaining <= 0)
            {
                _logger.LogInformation("Iteration count reached");
                return;
            }

            var tokensToRelease = _config.Throughput.Phases.GetTokensForPeriod(_startTime, _lastTime, now);
            _lastTime = now;

            var wholeTokens = Convert.ToInt32(tokensToRelease);
            _partialTokens += tokensToRelease - wholeTokens;
            if (_partialTokens >= 1)
            {
                wholeTokens += Convert.ToInt32(_partialTokens);
                _partialTokens -= Math.Truncate(_partialTokens);
            }
            
            if (_config.Throughput.Iterations > 0 && _iterationsRemaining - wholeTokens < 0)
            {
                wholeTokens = _iterationsRemaining;
            }

            _logger.LogDebug("Calculated tokens. {wholeTokens} tokens. {partialTokens} partialTokens", wholeTokens, _partialTokens);
            if (wholeTokens > 0)
            {
                _logger.LogDebug("Releasing {wholeTokens} tokens", wholeTokens);
                _stats?.Increment(wholeTokens, $"{_config.Listeners.Statsd.Bucket}.releasedtoken");
                _taskExecution.Release(wholeTokens);
            }
        }

        public async Task<bool> RequestTaskExecution(CancellationToken ct)
        {
            _logger.LogDebug($"Task execution request start");
            _stats?.Increment($"{_config.Listeners.Statsd.Bucket}.requesttaskexecutionstart");

            if (!RequestTaskContinuedExecution())
            {
                _logger.LogDebug($"Found kill token. dying.");
                _stats?.Increment($"{_config.Listeners.Statsd.Bucket}.requesttaskexecutionend");
                _stats?.Increment($"{_config.Listeners.Statsd.Bucket}.taskkill");
                return false;
            }
            if (_config.ThroughputEnabled)
            {
                _logger.LogDebug($"Task execution enabled, waiting");
                await _taskExecution.WaitAsync(ct);
                _logger.LogDebug($"Waiting complete");
            }

            int iterations = 0;
            if (_config.Throughput.Iterations > 0)
            {
                try
                {
                    await _taskDecrement.WaitAsync(ct);
                    iterations = Interlocked.Decrement(ref _iterationsRemaining);
                    _logger.LogDebug("Iterations: {iterations}. Iterations left: {iterationsRemaining}", _config.Throughput.Iterations, iterations);
                }
                finally
                {
                    _taskDecrement.Release();
                }
            }

            var isCompleted = IsTestComplete(_startTime, iterations);
            _logger.LogDebug("Is test completed: {isCompleted}. Iterations: {iterations}. Max iterations: {maxIterations}", isCompleted, iterations, _config.Throughput.Iterations);
            _stats?.Increment($"{_config.Listeners.Statsd.Bucket}.requesttaskexecutionend");
            return !isCompleted;
        }

        private bool IsTestComplete(DateTime startTime, int iterationsRemaining) =>
            _timeService.Now() >= _endTime || (iterationsRemaining < 0 && _config.Throughput.Iterations > 0);

        public void RequestTaskDiscontinues()
        {
            _taskKill.Enqueue(true);
            // _stats?.Increment(1, $"{_config.Listeners.Statsd.Bucket}.taskkillrequested");
        }

        public bool RequestTaskContinuedExecution()
        {
            // _stats?.Increment(1, $"{_config.Listeners.Statsd.Bucket}.taskcontinuerequested");
            if (_config.ThroughputEnabled)
            {
                if (_taskKill.TryDequeue(out var result))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
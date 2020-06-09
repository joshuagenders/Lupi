using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lupi.Configuration;
using Lupi.Listeners;

namespace Lupi 
{
    public class TestResultPublisher : ITestResultPublisher
    {
        private readonly ConcurrentQueue<TestResult> _results;
        private readonly List<ITestResultListener> _listeners;
        private readonly Config _config;

        public bool TestCompleted { get; set; }

        public TestResultPublisher(Config config)
        {
            _results = new ConcurrentQueue<TestResult>();
            _listeners = new List<ITestResultListener>();
            _config = config;
        }

        public void Publish(TestResult result) =>
            _results.Enqueue(result);

        public void Subscribe(ITestResultListener listener)
        {
            if (!_listeners.Contains(listener))
            {
                _listeners.Add(listener);
            }
        }

        public async Task Process(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested && !TestCompleted || _results.Any())
            {
                var resultProcessingBatchSize = 500;
                var count = 0;
                var batch = new List<TestResult>();
                while (_results.TryDequeue(out var result) && count < resultProcessingBatchSize)
                {
                    count++;
                    batch.Add(result);
                }
                var tasks = _listeners.Select(l => l.OnResult(batch.ToArray(), ct));
                await Task.WhenAll(tasks);
                if (!TestCompleted)
                {
                    await Task.Delay(_config.Engine.CheckInterval);
                }
            }
        }
    }

    public interface ITestResultPublisher
    {
        Task Process(CancellationToken ct);
        void Publish(TestResult result);
        void Subscribe(ITestResultListener listener);
        bool TestCompleted { get; set; }
    }
}
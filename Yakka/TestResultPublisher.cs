using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Yakka {
    public class TestResultPublisher 
    {
        private readonly ConcurrentQueue<TestResult> _results;
        private readonly List<ITestResultListener> _listeners;
        public bool TestCompleted { get; set; }

        public TestResultPublisher()
        {
            _results = new ConcurrentQueue<TestResult>();
            _listeners = new List<ITestResultListener>();
        }

        public void Publish(TestResult result) =>
            _results.Enqueue(result);

        public void Subscribe(ITestResultListener listener) =>
            _listeners.Add(listener);

        public async Task Process(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested && !TestCompleted || _results.Any())
            {
                if(_results.TryDequeue(out var result))
                {
                    var tasks = _listeners.Select(l => l.OnResult(result));
                    await Task.WhenAll(tasks);
                }
                if (!_results.Any() && !TestCompleted)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(250));
                }
            }
        }
    }
}
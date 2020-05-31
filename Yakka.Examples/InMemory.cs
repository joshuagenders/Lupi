using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Yakka.Examples
{
    public class InMemory
    {
        public int Counter;
        private readonly IInternalDependency _dependency;

        public InMemory(IInternalDependency dependency)
        {
            Counter = 0;
            _dependency = dependency;
        }

        public static int GetInt() => 42;
        public static int GetIntWithDependency(IInternalDependency dependency) => 
            dependency.GetData();

        public int Increment()
        {
            return Interlocked.Increment(ref Counter);
        }

        public async Task IncrementAsync(CancellationToken ct)
        {
            await Task.Delay(10);
            Interlocked.Increment(ref Counter);
        }

        public async Task<int> IncrementReturnAsync(CancellationToken ct)
        {
            await Task.Delay(10);
            return Interlocked.Increment(ref Counter);
        }

        public int DependencyRequired(IInternalDependency dependency)
        {
            var data = dependency.GetData();
            return data;
        }

        public int DependencyUsed()
        {
            var data = _dependency.GetData();
            return data;
        }

        public async Task<(Stopwatch, string)> TimedGetString(CancellationToken ct)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            await Task.Delay(10, ct);
            stopwatch.Stop();
            return (stopwatch, "timed get with string return");
        }

        public async Task<TimeSpan> TimedGetTimespan(CancellationToken ct)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            await Task.Delay(10, ct);
            stopwatch.Stop();
            return TimeSpan.FromMilliseconds(stopwatch.ElapsedMilliseconds);
        }

        public async Task<string> StringReturn(CancellationToken ct)
        {
            await Task.Delay(10, ct);
            return "example";
        }
    }
}

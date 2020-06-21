using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Lupi.Examples
{
    public class HttpClientExample
    {
        public async Task Get(CancellationToken ct)
        {
            var result = await GlobalState.HttpClient.GetAsync("https://<website>.com/", ct);
            result.EnsureSuccessStatusCode();
        }

        public async Task<Stopwatch> TimedGet(Stopwatch stopwatch, CancellationToken ct)
        {
            stopwatch.Start();
            await GlobalState.HttpClient.GetAsync("https://<website>.com/", ct);
            stopwatch.Stop();
            return stopwatch;
        }

        public async Task<(Stopwatch, string)> TimedGetString(CancellationToken ct)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            await GlobalState.HttpClient.GetAsync("https://<website>.com/", ct);
            stopwatch.Stop();
            return (stopwatch, "timed get with string return");
        }

        public async Task<TimeSpan> TimedGetTimespan(CancellationToken ct)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            await GlobalState.HttpClient.GetAsync("https://<website>.com/", ct);
            stopwatch.Stop();
            return TimeSpan.FromMilliseconds(stopwatch.ElapsedMilliseconds);
        }

        public async Task<(TimeSpan, string, bool)> GetWithReturnValuesAsync(CancellationToken ct)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var result = await GlobalState.HttpClient.GetAsync("https://<website>.com/", ct);
            var response = await result.Content.ReadAsStringAsync();
            stopwatch.Stop();
            return (stopwatch.Elapsed, response, result.IsSuccessStatusCode);
        }

        public async Task<string> StringReturn(CancellationToken ct)
        {
            await GlobalState.HttpClient.GetAsync("https://<website>.com/", ct);
            return "example";
        }
    }
}

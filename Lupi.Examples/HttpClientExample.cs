using FluentAssertions;
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
            var result = await GlobalState.HttpClient.GetAsync("https://blazedemo.com/", ct);
            result.EnsureSuccessStatusCode();
            var content = await result.Content.ReadAsStringAsync();
            content.Should().Contain("<h1>Welcome to the Simple Travel Agency!</h1>");
        }

        public async Task<Stopwatch> TimedGet(Stopwatch stopwatch, CancellationToken ct)
        {
            stopwatch.Start();
            await GlobalState.HttpClient.GetAsync("https://blazedemo.com/", ct);
            stopwatch.Stop();
            return stopwatch;
        }

        public async Task<(Stopwatch, string)> TimedGetString(CancellationToken ct)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            await GlobalState.HttpClient.GetAsync("https://blazedemo.com/", ct);
            stopwatch.Stop();
            return (stopwatch, "timed get with string return");
        }

        public async Task<TimeSpan> TimedGetTimespan(CancellationToken ct)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            await GlobalState.HttpClient.GetAsync("https://blazedemo.com/", ct);
            stopwatch.Stop();
            return TimeSpan.FromMilliseconds(stopwatch.ElapsedMilliseconds);
        }

        public async Task<string> StringReturn(CancellationToken ct)
        {
            await GlobalState.HttpClient.GetAsync("https://blazedemo.com/", ct);
            return "example";
        }
    }
}

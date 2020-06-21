using FluentAssertions;
using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

namespace Lupi.Examples
{
    public class PuppeteerExample
    {
        public async Task<TimeSpan> Homepage()
        {
            using (var page = await GlobalState.Browser.NewPageAsync())
            {
                var watch = new Stopwatch();
                watch.Start();
                await page.GoToAsync("https://<website>.com/");
                await page.WaitForSelectorAsync("div.container");
                watch.Stop();
                return watch.Elapsed;
            }
        }
    }
}

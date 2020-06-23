using System;
using System.Diagnostics;
using System.Threading.Tasks;
using PuppeteerSharp;

namespace Lupi.Examples
{
    public class PuppeteerExample
    {
        public async Task<(TimeSpan, bool)> Homepage()
        {
            using (var page = await GlobalState.Browser.NewPageAsync())
            {
                var watch = new Stopwatch();
                watch.Start();
                var response = await page.GoToAsync("https://<website>.com", WaitUntilNavigation.DOMContentLoaded);
                watch.Stop();
                return (watch.Elapsed, response.Ok);
            }
        }
    }
}

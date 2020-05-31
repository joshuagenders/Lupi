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
                await page.GoToAsync("https://blazedemo.com/");
                await page.WaitForSelectorAsync("div.container");
                watch.Stop();
                var title = await page.EvaluateExpressionAsync<dynamic>("document.getElementsByTagName('h1')[0].innerText");
                title.Should().Be("Welcome to the Simple Travel Agency!");
                return watch.Elapsed;
            }
        }
    }
}

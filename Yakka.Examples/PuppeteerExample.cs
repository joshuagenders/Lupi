using FluentAssertions;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Yakka.Examples
{
    public class PuppeteerExample
    {
        public async Task Homepage()
        {
            var page = await GlobalState.Browser.NewPageAsync();
            var response = await page.GoToAsync("https://blazedemo.com/");
            response.Status.Should().Be(HttpStatusCode.OK);
        }
    }
}

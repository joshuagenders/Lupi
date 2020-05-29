using PuppeteerSharp;
using System.Net.Http;
using System.Threading.Tasks;

namespace Yakka.Examples
{
    public static class GlobalState
    {
        public static HttpClient HttpClient { get; private set; } = new HttpClient();
        public static Browser Browser { get; private set; }

        public async static Task Init()
        {
            await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultRevision);
            Browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true
            });
        }

        public static async Task Teardown()
        {
            await Browser?.CloseAsync();
        }
    }
}

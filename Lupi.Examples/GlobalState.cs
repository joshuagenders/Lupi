using Microsoft.Extensions.DependencyInjection;
using PuppeteerSharp;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Lupi.Examples
{
    public static class GlobalState
    {
        public static HttpClient HttpClient { get; private set; } = new HttpClient();
        public static Browser Browser { get; private set; }

        public async static Task InitBrowser()
        {
            await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultChromiumRevision);
            Browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                Args = new []{
                    "--disable-web-security",
                    "--disable-background-networking",
                    "--disable-background-timer-throttling",
                    "--disable-backgrounding-occluded-windows",
                    "--disable-breakpad",
                    "--disable-client-side-phishing-detection",
                    "--disable-default-apps",
                    "--disable-extensions",
                    "--disable-features=site-per-process",
                    "--disable-prompt-on-repost",
                    "--disable-renderer-backgrounding",
                    "--disable-sync",
                    "--disable-translate",
                    "--metrics-recording-only",
                    "--safebrowsing-disable-auto-update",
                    "--mute-audio",
                    "--disable-gpu",
                    "--no-sandbox",
                    "--disable-setuid-sandbox",
                    "--user-data-dir"
                },
                Timeout = 10000,
            });
        }

        public static async Task TeardownBrowser()
        {
            await Browser?.CloseAsync();
        }

        public static IServiceProvider BuildServiceProvider() =>
            new ServiceCollection()
                .AddTransient<IInternalDependency, InternalDependency>()
                .BuildServiceProvider();
    }
}

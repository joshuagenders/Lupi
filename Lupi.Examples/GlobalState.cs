using Autofac;
using PuppeteerSharp;
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
            await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultRevision);
            Browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true
            });
        }

        public static async Task TeardownBrowser()
        {
            await Browser?.CloseAsync();
        }

        public static ContainerBuilder Startup(ContainerBuilder builder)
        {
            builder.RegisterAssemblyTypes(typeof(GlobalState).Assembly);
            return builder;
        }
    }
}

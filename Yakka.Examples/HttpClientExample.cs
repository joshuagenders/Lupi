using System.Threading.Tasks;

namespace Yakka.Examples
{
    public class HttpClientExample
    {
        public async Task Get()
        {
            var result = await GlobalState.HttpClient.GetAsync("https://blazedemo.com/");
            result.EnsureSuccessStatusCode();
        }
    }
}

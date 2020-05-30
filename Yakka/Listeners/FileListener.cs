using Newtonsoft.Json;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Yakka.Configuration;

namespace Yakka.Listeners
{
    public class FileListener : ITestResultListener
    {
        private readonly Config _config;

        public FileListener(Config config)
        {
            _config = config;
        }

        public async Task OnResult(TestResult[] results, CancellationToken ct)
        {
            var lines = results.Select(JsonConvert.SerializeObject);
            await System.IO.File.AppendAllLinesAsync(_config.Listeners.File.Path, lines, ct);
        }
    }
}

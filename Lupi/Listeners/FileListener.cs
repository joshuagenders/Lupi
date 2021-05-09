using Newtonsoft.Json;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lupi.Configuration;
using Lupi.Results;

namespace Lupi.Listeners
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
            var lines = string.IsNullOrWhiteSpace(_config.Listeners.File.Format)
                ? results.Select(JsonConvert.SerializeObject)
                : results.Select(_config.Listeners.File.Format.FormatWith);
            await System.IO.File.AppendAllLinesAsync(_config.Listeners.File.Path, lines, ct);
        }
    }
}

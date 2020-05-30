using Newtonsoft.Json;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Yakka.Listeners
{
    public class ConsoleListener : ITestResultListener
    {
        public async Task OnResult(TestResult[] results, CancellationToken ct)
        {
            foreach (var result in results)
            {
                Console.WriteLine(JsonConvert.SerializeObject(result));
            }
            await Task.CompletedTask;
        }
    }
}

using System.Threading;
using System.Threading.Tasks;

namespace Yakka.Listeners
{
    public interface ITestResultListener
    {
        Task OnResult(TestResult[] results, CancellationToken ct);
    }
}

using System.Threading;
using System.Threading.Tasks;

namespace Lupi.Listeners
{
    public interface ITestResultListener
    {
        Task OnResult(TestResult[] results, CancellationToken ct);
    }
}

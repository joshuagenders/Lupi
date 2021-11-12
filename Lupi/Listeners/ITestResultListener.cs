using Lupi.Results;

namespace Lupi.Listeners
{
    public interface ITestResultListener
    {
        Task OnResult(TestResult[] results, CancellationToken ct);
    }
}

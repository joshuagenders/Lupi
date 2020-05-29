using System.Threading.Tasks;

namespace Yakka
{
    public interface ITestResultListener
    {
        Task OnResult(TestResult result);
    }
}

using System.Threading;
using System.Threading.Tasks;

namespace Lupi.Listeners
{
    public interface IAggregatorListener
    {
        Task OnResult(AggregatedResult result, CancellationToken ct);
    }
}

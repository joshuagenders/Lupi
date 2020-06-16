using Lupi.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Lupi.Listeners
{
    public class ExitConditionAggregatorListener : IAggregatorListener
    {
        private readonly Config _config;
        private readonly ExitSignal _exitSignal;
        private readonly Dictionary<int, Queue<bool>> _resultCache;

        public ExitConditionAggregatorListener(Config config, ExitSignal exitSignal)
        {
            _config = config;
            _exitSignal = exitSignal;
            _resultCache = new Dictionary<int, Queue<bool>>();
        }

        public async Task OnResult(AggregatedResult result, CancellationToken ct)
        {
            foreach (var c in _config.ExitConditions.Select((condition, index) => (condition, index)))
            {
                //todo
                //get prop
                //switch operator
                //  test value
                //  store result in cache -> removing old from queue when too long
                // for duration, calculate length from int (duration/aggregationperiod + 1)
            }
            //go through cache and check for any that break rules, 
            foreach (var r in _resultCache)
            {

            }
            if (_resultCache.Any(r => r.Value.All(r => r)))
            {
                _exitSignal.Signal("the reason");
            }
            await Task.CompletedTask;
        }
    }
}

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
        private readonly IExitSignal _exitSignal;
        private readonly Dictionary<int, Queue<(bool passed, double value)>> _resultCache;
        private readonly Dictionary<string, Func<double, double, bool>> operatorFunctions = 
            new Dictionary<string, Func<double, double, bool>>
            {
                { "<", new Func<double, double, bool>((a, b) => a < b) },
                { ">", new Func<double, double, bool>((a, b) => a > b) },
                { ">=", new Func<double, double, bool>((a, b) => a >= b) },
                { "<=", new Func<double, double, bool>((a, b) => a <= b) },
                { "=", new Func<double, double, bool>((a, b) => a == b) }
            };

        public ExitConditionAggregatorListener(Config config, IExitSignal exitSignal)
        {
            _config = config;
            _exitSignal = exitSignal;
            _resultCache = new Dictionary<int, Queue<(bool, double)>>();
        }

        public async Task OnResult(AggregatedResult result, CancellationToken ct)
        {
            var exitConditions = _config
                .ExitConditions
                .Select((condition, index) => (condition, index))
                .Select(c => new {
                    c.condition,
                    c.index,
                    maxResults = c.condition.Periods > 0
                        ? c.condition.Periods
                        : Convert.ToInt32(
                            c.condition.Duration.TotalMilliseconds / _config.Engine.AggregationInterval.TotalMilliseconds + 1)
                })
                .Where(e => operatorFunctions.ContainsKey(e.condition.Operator))
                .ToList();

            foreach (var c in exitConditions)
            {
                var propertyValue = result.GetType().GetProperty(c.condition.Property)?.GetValue(result);
                if (propertyValue == null)
                {
                    continue;
                }
                
                double value;
                try 
                {
                    value = Convert.ToDouble(propertyValue);
                }
                catch (Exception)
                {
                    continue;
                }
                
                var predicateResult = operatorFunctions[c.condition.Operator](value, c.condition.Value);

                if (!_resultCache.ContainsKey(c.index))
                {
                    _resultCache[c.index] = new Queue<(bool, double)>();
                }
                
                _resultCache[c.index].Enqueue((predicateResult, value));
                
                if (_resultCache[c.index].Count > c.maxResults)
                {
                    _resultCache[c.index].Dequeue();
                }
                
                if (_resultCache[c.index].Count == c.maxResults)
                {
                    //assess if condition is met
                    if (_resultCache[c.index].All(r => r.passed))
                    {
                        var conditionText = YamlHelper.Serialize(c.condition);

                        _exitSignal.Signal(
                            $"Exit Condition was triggered.\nCondition:\n{conditionText}",
                            c.condition.PassedFailed.Equals("passed", StringComparison.InvariantCultureIgnoreCase));
                        break;
                    }                    
                }
            }
            await Task.CompletedTask;
        }
    }
}

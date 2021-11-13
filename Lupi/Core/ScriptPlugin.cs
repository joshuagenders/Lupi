using Lupi.Configuration;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace Lupi.Core
{
    public class ScriptPlugin : IPlugin
    {
        private readonly Config _config;
        private readonly CancellationToken _ct;
        private readonly Dictionary<string, Script<object>> _compiledScripts = new();
        private readonly Dictionary<string, ScriptState<object>> _scriptStates = new();
        private object _globals { get; set; }

        public ScriptPlugin(Config config, CancellationToken ct = default)
        {
            _config = config;
            _ct = ct;
            _compiledScripts = config.Scripting.Scripts.ToDictionary(
                script => script.Key,
                script => CSharpScript.Create(script.Value.Script)
            );
        }

        public async Task<object> ExecuteSetupMethod()
        {
            dynamic theGlobals = new System.Dynamic.ExpandoObject();
            theGlobals.ct = _ct;
            foreach (var globalValue in _config.Scripting.Globals){
                var resultState = await CSharpScript.RunAsync(globalValue.Value.Script);
                (theGlobals as IDictionary<string, Object>).Add(globalValue.Key, resultState.ReturnValue);
            }
            theGlobals.ct = _ct;
            _globals = theGlobals;
            return await Task.FromResult(true);
        }

        public Task<object> ExecuteTeardownMethod()
        {
            // todo, figure out if implementing this in scripts is required
            throw new NotImplementedException();
        }

        public async Task<object> ExecuteTestMethod()
        {
            List<object> results = new();
            foreach (var step in _config.Scripting.Scenario){
                if (_compiledScripts.ContainsKey(step)){
                    var resultState = await _compiledScripts[step].RunAsync(_globals, _ct);
                    results.Add(resultState.ReturnValue);
                }
            }
            return results;
        }
    }
}
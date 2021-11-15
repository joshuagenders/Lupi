using Lupi.Configuration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.Reflection;

namespace Lupi.Core
{
    public class GlobalRefs
    {
        public dynamic __ { get; set; }
    }
    public class ScriptPlugin : IPlugin
    {
        private readonly Config _config;
        private readonly CancellationToken _ct;
        private Dictionary<string, Script<object>> _compiledScripts = new();
        private GlobalRefs _globals { get; set; }

        public ScriptPlugin(Config config, CancellationToken ct = default)
        {
            _config = config;
            _ct = ct;
        }

        public async Task<object> ExecuteSetupMethod()
        {
            if (_globals != null){
                return await Task.FromResult(true);
            }
            dynamic globals = new System.Dynamic.ExpandoObject();
            globals.ct = _ct;
            foreach (var globalValue in _config.Scripting.Globals){
                var resultState = await CSharpScript.RunAsync(
                    globalValue.Value.Script,
                    ScriptOptions.Default.WithImports(globalValue.Value.Imports ?? new List<string>())
                );
                (globals as IDictionary<string, Object>).Add(globalValue.Key, resultState.ReturnValue);
            }
            globals.ct = _ct;
            _globals = new GlobalRefs { __ = globals };

            // config validation - move elsewhere?
            var invalidSteps = _config.Scripting.Scenario.Where(s => !_config.Scripting.Scripts.ContainsKey(s));
            if (invalidSteps.Any())
            {
                throw new ArgumentException($"Could not find scenario key(s) in scripting.scripts. {string.Join(", ", invalidSteps)}");
            }
            try
            {
                var refs = new List<MetadataReference>
                {
                    MetadataReference.CreateFromFile(typeof(Microsoft.CSharp.RuntimeBinder.RuntimeBinderException).GetTypeInfo().Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(System.Runtime.CompilerServices.DynamicAttribute).GetTypeInfo().Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(System.Net.Http.HttpClient).GetTypeInfo().Assembly.Location)
                };

                _compiledScripts = _config.Scripting.Scripts.ToDictionary(
                    script => script.Key,
                    script => CSharpScript.Create(
                        script.Value.Script,
                        ScriptOptions.Default.WithImports(script.Value.Imports).AddReferences(refs),
                        globalsType: typeof(GlobalRefs)
                    )
                );
            }
            catch (CompilationErrorException e)
            {
                Console.WriteLine("Error compiling scripts");
                Console.WriteLine(string.Join(Environment.NewLine, e.Diagnostics));
                System.Diagnostics.Debug.WriteLine(string.Join(Environment.NewLine, e.Diagnostics));
                throw e;
            }

            return await Task.FromResult(true);
        }

        public async Task<object> ExecuteTeardownMethod()
        {
            // todo, figure out if implementing this in scripts is required
            return await Task.FromResult(true);
        }

        public async Task<object> ExecuteTestMethod()
        {
            if (_config.Scripting.Scripts.Count == 1 && !_config.Scripting.Scenario.Any()){
                return await this.ExecuteScript(_config.Scripting.Scripts.Single().Key);
            }
            List<object> results = new();
            foreach (var step in _config.Scripting.Scenario){
                results.Add(await this.ExecuteScript(step));
            }
            return results.Count == 1 ? results.Single() : results;
        }

        private async Task<object> ExecuteScript(string step){
            var resultState = await _compiledScripts[step].RunAsync(_globals, _ct);
            return resultState.ReturnValue;
        }
    }
}
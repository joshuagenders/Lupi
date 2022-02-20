using Lupi.Configuration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.Reflection;
using System.IO;

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
            if (_globals != null)
            {
                return await Task.FromResult(true);
            }
            dynamic globals = new System.Dynamic.ExpandoObject();
            globals.ct = _ct;
            _globals = new GlobalRefs { __ = globals };
            foreach (var globalValue in _config.Scripting.Globals)
            {
                var script = CompileScript(globalValue.Value);
                var resultState = await script.RunAsync(_globals, _ct);
                (globals as IDictionary<string, Object>).Add(globalValue.Key, resultState.ReturnValue);
            }
            globals.ct = _ct;

            try
            {
                _compiledScripts = _config.Scripting.Scripts.ToDictionary(
                    script => script.Key,
                    script => CompileScript(script.Value)
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
            // todo, implement continuation on globals by global var name
            return await Task.FromResult(true);
        }

        private Script<object> CompileScript(LupiScript script)
        {

            var refs = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(Microsoft.CSharp.RuntimeBinder.RuntimeBinderException).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Runtime.CompilerServices.DynamicAttribute).GetTypeInfo().Assembly.Location)
            };

            foreach (var import in script.Imports)
            {
                try
                {
                    var assemblyReference = Assembly.Load(import);
                    refs.Add(MetadataReference.CreateFromFile(assemblyReference.Location));
                }
                catch
                {
                    // todo log/output
                }
            }

            if (script.References?.Any() ?? false)
            {
                var fileRefs = script.References.Where(System.IO.File.Exists);
                var folderRefs = script.References.Where(System.IO.Directory.Exists);

                refs.AddRange(fileRefs.Select(path => MetadataReference.CreateFromFile(path)));
                foreach (var folderRef in folderRefs)
                {
                    refs.AddRange(
                        System.IO.Directory
                            .GetFiles(folderRef, "*.*")
                            .Where(f => f.ToLowerInvariant().EndsWith("dll"))
                            .Select(path => MetadataReference.CreateFromFile(path))
                    );
                }
            }

            return CSharpScript.Create(
                script.Script,
                ScriptOptions.Default.WithImports(script.Imports).AddReferences(refs),
                globalsType: typeof(GlobalRefs));
        }

        public async Task<object> ExecuteTestMethod()
        {
            if (_config.Scripting.Scripts.Count == 1 && !_config.Scripting.Scenario.Any())
            {
                return await this.ExecuteScript(_config.Scripting.Scripts.Single().Key);
            }
            List<object> results = new();
            foreach (var step in _config.Scripting.Scenario)
            {
                results.Add(await this.ExecuteScript(step));
            }
            return results.Count == 1 ? results.Single() : results;
        }

        private async Task<object> ExecuteScript(string step)
        {
            var resultState = await _compiledScripts[step].RunAsync(_globals, _ct);
            return resultState.ReturnValue;
        }
    }
}
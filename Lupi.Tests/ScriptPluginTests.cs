using System.Diagnostics;
using Lupi.Configuration;
using Lupi.Core;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.Logging;

namespace Lupi.Tests
{
    [Collection("Sequential")]
    public class ScriptPluginTests
    {
        // TODO:
        // multiple threads accessing globals (entrypoint from TestRunner or Application)?
        // number of loaded assemblies / memory usage?

        [Fact]
        public async Task SimpleScriptReturnsCorrectValue()
        {
            var scripts = new[] { ("simple", "return 1 + 1;") };
            var config = GetConfig(scripts);
            var cts = new CancellationTokenSource();
            var sut = new ScriptPlugin(config, Mock.Of<ILogger<ScriptPlugin>>(), cts.Token);
            await sut.ExecuteSetupMethod();
            var result = await sut.ExecuteTestMethod();
            result.Should().Be(2);
        }

        [Fact]
        public async Task CanExecuteMultipleSteps()
        {
            var scripts = new[] {
                ("Step 1", "return 1 + 1;"),
                ("Step 2", "return 5 + 10;")
            };
            var config = GetConfig(scripts);
            config.Scripting.Scenario = new List<string>
            {
                "Step 1",
                "Step 2"
            };
            var cts = new CancellationTokenSource();
            var sut = new ScriptPlugin(config, Mock.Of<ILogger<ScriptPlugin>>(), cts.Token);
            await sut.ExecuteSetupMethod();
            var result = await sut.ExecuteTestMethod();
            var castedResult = ((IEnumerable<object>)result).ToList();
            castedResult.Count.Should().Be(2);
            castedResult[0].Should().Be(2);
            castedResult[1].Should().Be(15);
        }

        [Fact]
        public async Task CanReturnInstanceOfUserDefinedClass()
        {
            var script = @"
            class MyClass {
                public override string ToString() => ""SomeValue"";
            }
            return new MyClass();
            ";
            var scripts = new[] { ("createAClass", script) };
            var config = GetConfig(scripts);
            var cts = new CancellationTokenSource();
            var sut = new ScriptPlugin(config, Mock.Of<ILogger<ScriptPlugin>>(), cts.Token);
            await sut.ExecuteSetupMethod();
            var result = await sut.ExecuteTestMethod();
            result.ToString().Should().Be("SomeValue");
        }

        [Fact]
        public async Task CanReferenceThirdPartyLibrary()
        {
            var script = @"
            using (var reader = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(""v1,v2"")))){
                return new CsvReader(reader, CultureInfo.InvariantCulture);
            }";
            var scripts = new[] { ("Reference a third-party library", script) };
            var config = GetConfig(scripts);
            var s = config.Scripting.Scripts.First().Value;
            s.References = new[] { "./CsvHelper.dll" };
            s.Imports = new[] { "CsvHelper", "System", "System.IO", "System.Text", "System.Globalization" };
            var builtConfig = ConfigHelper.Build(config, ".");
            var cts = new CancellationTokenSource();
            var sut = new ScriptPlugin(builtConfig, Mock.Of<ILogger<ScriptPlugin>>(), cts.Token);
            await sut.ExecuteSetupMethod();
            var result = await sut.ExecuteTestMethod();
            result.GetType().Name.Should().Be("CsvReader");
        }

        [Fact]
        public async Task CanReferenceThirdPartyLibraryByFolder()
        {
            var script = @"
            using (var reader = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(""v1,v2"")))){
                return new CsvReader(reader, CultureInfo.InvariantCulture);
            }";
            var scripts = new[] { ("Reference a third-party library", script) };
            var config = GetConfig(scripts);
            var s = config.Scripting.Scripts.First().Value;
            s.References = new[] { "./" };
            s.Imports = new[] { "CsvHelper", "System", "System.IO", "System.Text", "System.Globalization" };
            var builtConfig = ConfigHelper.Build(config, ".");
            var cts = new CancellationTokenSource();
            var sut = new ScriptPlugin(builtConfig, Mock.Of<ILogger<ScriptPlugin>>(), cts.Token);
            await sut.ExecuteSetupMethod();
            var result = await sut.ExecuteTestMethod();
            result.GetType().Name.Should().Be("CsvReader");
        }

        [Fact]
        public async Task CanAccessInstanceOfUserDefinedClassInGlobals()
        {
            var globalScript = @"
            class MyClass {
                public override string ToString() => ""SomeValue"";
            }
            return new MyClass();
            ";
            var script = "return __.myVarName.ToString();";
            var scripts = new[] { ("createAClass", script) };
            var config = GetConfig(scripts);
            config.Scripting.Globals = new Dictionary<string, LupiScript>
            {
                { "myVarName", new LupiScript { Script = globalScript, Imports = new List<string> { } } }
            };
            var cts = new CancellationTokenSource();
            var sut = new ScriptPlugin(config, Mock.Of<ILogger<ScriptPlugin>>(), cts.Token);
            await sut.ExecuteSetupMethod();
            var result = await sut.ExecuteTestMethod();
            result.ToString().Should().Be("SomeValue");
        }

        [Fact]
        public async Task CanCreateSharedHttpClient()
        {
            var globalScript = "return new System.Net.Http.HttpClient();";
            var script = "return __.httpClient.GetType().Name;";
            var scripts = new[] { ("Get a HTTP client", script) };
            var config = GetConfig(scripts);
            config.Scripting.Globals = new Dictionary<string, LupiScript>
            {
                { "httpClient", new LupiScript { Script = globalScript, Imports = new List<string> { "System", "System.Net.Http" } } }
            };
            var cts = new CancellationTokenSource();
            var sut = new ScriptPlugin(config, Mock.Of<ILogger<ScriptPlugin>>(), cts.Token);
            await sut.ExecuteSetupMethod();
            var result = await sut.ExecuteTestMethod();
            result.Should().Be("HttpClient");
        }

        [Fact]
        [Trait("Category", "Flaky")]
        public async Task ScriptsRespectCancellationToken()
        {
            var cancellationOccurred = false;
            var scripts = new[] { ("simple", "await Task.Delay(3000, __.ct);") };
            var config = GetConfig(scripts);
            config.Scripting.Scripts.First().Value.Imports.Append("System.Threading.Tasks");
            config.Scripting.Globals.Add("somevalue", new LupiScript
            {
                Script = "Task.CompletedTask",
                Imports = new List<string>() { "System.Threading.Tasks" }
            }); // not sure why, but required to get test to pass when running all
            var cts = new CancellationTokenSource();
            var sut = new ScriptPlugin(config, Mock.Of<ILogger<ScriptPlugin>>(), cts.Token);
            var timer = new Stopwatch();
            cts.CancelAfter(TimeSpan.FromMilliseconds(150));
            try
            {
                timer.Start();
                await sut.ExecuteSetupMethod();
                await sut.ExecuteTestMethod();
            }
            catch (OperationCanceledException)
            {
                cancellationOccurred = true;
            }
            finally
            {
                timer.Stop();
            }
            cancellationOccurred.Should().BeTrue();
            timer.ElapsedMilliseconds.Should().BeLessThan(2850);
        }

        [Fact]
        public async Task ScriptReturnsCompilationErrors()
        {
            var scripts = new[] { ("simple", "return 1 + ;") };
            var config = GetConfig(scripts);
            var cts = new CancellationTokenSource();
            var sut = new ScriptPlugin(config, Mock.Of<ILogger<ScriptPlugin>>(), cts.Token);
            await sut.ExecuteSetupMethod();
            Func<Task> act = () => sut.ExecuteTestMethod();
            await act.Should().ThrowAsync<CompilationErrorException>();
        }

        private Config GetConfig(IEnumerable<(string name, string script)> scripts)
        {
            return new Config
            {
                Scripting = new Scripting
                {
                    Scripts = scripts.ToDictionary(
                        k => k.name,
                        v => new LupiScript
                        {
                            Script = v.script,
                            Imports = Array.Empty<string>()
                        }
                    )
                }
            };
        }
    }
}
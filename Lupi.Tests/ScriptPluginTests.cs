using Lupi.Configuration;
using Lupi.Core;

namespace Lupi.Tests {
    public class ScriptPluginTests {
        // referencing custom classes created in globals
        // scripts respecting cancellationtoken timeout
        // adding references
        // only providing script no scenario
        // scenario and script provided
        // compilation errors surfaced
        [Fact]
        public async Task SimpleScriptReturnsCorrectValue(){
            var scripts = new[] { ("simple", "return 1 + 1;") };
            var config = GetConfig(scripts);
            var cts = new CancellationTokenSource();
            var sut = new ScriptPlugin(config, cts.Token);
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
            var sut = new ScriptPlugin(config, cts.Token);
            await sut.ExecuteSetupMethod();
            var result = await sut.ExecuteTestMethod();
            var castedResult = ((IEnumerable<object>)result).ToList();
            castedResult.Count.Should().Be(2);
            castedResult[0].Should().Be(2);
            castedResult[1].Should().Be(15);
        }

        [Fact]
        public async Task CanReturnInstanceOfUserDefinedClass(){
            var script = @"
            class MyClass {
                public override string ToString() => ""SomeValue"";
            }
            return new MyClass();
            ";
            var scripts = new[] { ("createAClass", script) };
            var config = GetConfig(scripts);
            var cts = new CancellationTokenSource();
            var sut = new ScriptPlugin(config, cts.Token);
            await sut.ExecuteSetupMethod();
            var result = await sut.ExecuteTestMethod();
            result.ToString().Should().Be("SomeValue");
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
            var script = "return g.myVarName.ToString();";
            var scripts = new[] { ("createAClass", script) };
            var config = GetConfig(scripts);
            config.Scripting.Globals = new Dictionary<string, LupiScript>
            {
                { "myVarName", new LupiScript { Script = globalScript, Imports = new List<string> { } } }
            };
            var cts = new CancellationTokenSource();
            var sut = new ScriptPlugin(config, cts.Token);
            await sut.ExecuteSetupMethod();
            var result = await sut.ExecuteTestMethod();
            result.ToString().Should().Be("SomeValue");
        }

        private Config GetConfig(IEnumerable<(string name, string script)> scripts)
        {
            return new Config
            {
                Scripting = new Scripting
                {
                    Scripts = scripts.ToDictionary(
                        k => k.name,
                        v => new LupiScript{
                            Script = v.script,
                            Imports = Array.Empty<string>()
                        }
                    )
                }
            };
        }
    }
}
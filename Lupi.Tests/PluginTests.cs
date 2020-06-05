using FluentAssertions;
using System;
using System.Diagnostics;
using Xunit;
using Lupi.Configuration;
using System.Threading.Tasks;

namespace Lupi.Tests
{
    public class PluginTests
    {
        [Theory]
        [InlineData("Increment", 1)]
        [InlineData("IncrementAsync", null)]
        [InlineData("IncrementReturnAsync", 1)]
        public async Task WhenMethodExecuted_ReturnTypeIsCorrect(string method, object expectedResult)
        {
            var config = GetConfig(method);
            var plugin = new Plugin(config);
            var result = await plugin.ExecuteTestMethod();
            result.Should().Be(expectedResult);
        }

        [Fact]
        public async Task WhenTupleReturned_BothValuesCanBeAccessed()
        {
            var config = GetConfig("TimedGetString");
            var plugin = new Plugin(config);
            var result = await plugin.ExecuteTestMethod();
            var casted = (ValueTuple<Stopwatch, string>) result;
            casted.Item1.ElapsedMilliseconds.Should().BeGreaterThan(0);
            casted.Item2.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task StaticMethodsCanBeExecuted()
        {
            var config = GetConfig("GetInt");
            var plugin = new Plugin(config);
            var result = await plugin.ExecuteTestMethod();
            result.Should().Be(42);
        }

        [Fact]
        public async Task StaticMethodsWithDependenciesCanBeExecuted()
        {
            var config = GetConfig("GetIntWithDependency");
            var plugin = new Plugin(config);
            var result = (int)await plugin.ExecuteTestMethod();
            result.Should().BeInRange(1, 100);
        }

        [Fact]
        public async Task SetupMethodCanBeExecuted()
        {
            var config = GetConfig("GetIntWithDependency");
            config.Test.SetupClass = config.Test.TestClass;
            config.Test.SetupMethod = config.Test.TestMethod;
            var plugin = new Plugin(config);
            var result = (int)await plugin.ExecuteSetupMethod();
            result.Should().BeInRange(1, 100);
        }

        [Fact]
        public async Task TeardownCanBeExecuted()
        {
            var config = GetConfig("GetIntWithDependency");
            config.Test.TeardownClass = config.Test.TestClass;
            config.Test.TeardownMethod = config.Test.TestMethod;
            var plugin = new Plugin(config);
            var result = (int)await plugin.ExecuteTeardownMethod();
            result.Should().BeInRange(1, 100);
        }

        private Config GetConfig(string method) => new Config
        {
            Concurrency = new Concurrency
            {
                Threads = 1
            },
            Test = new Test
            {
                AssemblyPath = "Examples/Lupi.Examples.dll",
                TestClass = "Lupi.Examples.InMemory",
                TestMethod = method
            },
            Throughput = new Throughput
            {
                Iterations = 1,
                HoldFor = TimeSpan.FromSeconds(1)
            }
        };
    }
}

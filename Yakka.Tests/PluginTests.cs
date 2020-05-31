using FluentAssertions;
using System;
using System.Diagnostics;
using Xunit;
using Yakka.Configuration;

namespace Yakka.Tests
{
    public class PluginTests
    {
        [Theory]
        [InlineData("Increment", 1)]
        [InlineData("IncrementAsync", null)]
        [InlineData("IncrementReturnAsync", 1)]
        public void WhenMethodExecuted_ReturnTypeIsCorrect(string method, object expectedResult)
        {
            var config = GetConfig(method);
            var plugin = new Plugin(config);
            var result = plugin.ExecuteTestMethod();
            result.Should().Be(expectedResult);
        }

        [Fact]
        public void WhenTupleReturned_BothValuesCanBeAccessed()
        {
            var config = GetConfig("TimedGetString");
            var plugin = new Plugin(config);
            var result = plugin.ExecuteTestMethod();
            var casted = (ValueTuple<Stopwatch, string>)result;
            casted.Item1.ElapsedMilliseconds.Should().BeGreaterThan(0);
            casted.Item2.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void StaticMethodsCanBeExecuted()
        {
            var config = GetConfig("GetInt");
            var plugin = new Plugin(config);
            var result = plugin.ExecuteTestMethod();
            result.Should().Be(42);
        }

        [Fact]
        public void StaticMethodsWithDependenciesCanBeExecuted()
        {
            var config = GetConfig("GetIntWithDependency");
            var plugin = new Plugin(config);
            var result = (int)plugin.ExecuteTestMethod();
            result.Should().BeInRange(1, 100);
        }

        [Fact]
        public void SetupMethodCanBeExecuted()
        {
            var config = GetConfig("GetIntWithDependency");
            config.Test.AssemblySetupClass = config.Test.TestClass;
            config.Test.AssemblySetupMethod = config.Test.TestMethod;
            var plugin = new Plugin(config);
            var result = (int)plugin.ExecuteSetupMethod();
            result.Should().BeInRange(1, 100);
        }

        [Fact]
        public void TeardownCanBeExecuted()
        {
            var config = GetConfig("GetIntWithDependency");
            config.Test.AssemblyTeardownClass = config.Test.TestClass;
            config.Test.AssemblyTeardownMethod = config.Test.TestMethod;
            var plugin = new Plugin(config);
            var result = (int)plugin.ExecuteTeardownMethod();
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
                AssemblyPath = "Examples/Yakka.Examples.dll",
                TestClass = "Yakka.Examples.InMemory",
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

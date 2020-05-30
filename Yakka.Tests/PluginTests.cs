using FluentAssertions;
using System;
using System.Diagnostics;
using Xunit;

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

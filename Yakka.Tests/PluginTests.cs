using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Yakka.Tests
{
    public class PluginTests
    {
        [Fact]
        public void WhenPluginIsLoaded_MethodCanBeExecuted()
        {
            var config = new Config
            {
                Concurrency = new Concurrency
                {
                    Threads = 1
                },
                Test = new Test
                {
                    AssemblyPath = "Examples/Yakka.Examples.dll",
                    TestClass = "Yakka.Examples.InMemory",
                    TestMethod = "Increment"
                },
                Throughput = new Throughput
                {
                    Iterations = 1,
                    HoldFor = TimeSpan.FromSeconds(2)
                }
            };
            var plugin = new Plugin(config);
            var result = plugin.ExecuteTestMethod();
            result.Should().Be(1);
        }
    }
}

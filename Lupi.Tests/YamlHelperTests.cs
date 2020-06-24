using FluentAssertions;
using System;
using Xunit;
using Lupi.Configuration;

namespace Lupi.Tests
{
    public class YamlHelperTests
    {
        #region ConfigStrings
        private static string AllConfigBasic = @"
test:
    assemblyPath: path/to/my.dll
    singleTestClassInstance: true
    testClass: MyNamespace.MyClass # optional if method name is unique in the assembly
    testMethod: MyMethod
    setupClass: MyNamespace.SetupClass
    setupMethod: Init
    teardownClass: MyNamespace.TeardownClass
    teardownMethod: Teardown
concurrency:
    threads: 10 
    rampUp: 2m
    holdFor: 20s
    rampDown: 2m
    openWorkload: true # (can add additional threads when throughput is not met)
    minThreads: 3 # requires open workload
    maxThreads: 15 # requires open workload
throughput:
    tps: 20
    rampUp: 20s
    holdFor: 10m
    rampDown: 2m
    thinkTime: 500ms
";
        private static string AllConfigPhases = @"
test:
    assemblyPath: path/to/my.dll
    singleTestClassInstance: true
    testClass: MyNamespace.MyClass # optional if method name is unique in the assembly
    testMethod: MyMethod
    setupClass: MyNamespace.SetupClass
    setupMethod: Init
    teardownClass: MyNamespace.TeardownClass
    teardownMethod: Teardown
concurrency:
    threads: 10
    rampUp: 2m
    holdFor: 30s
    rampDown: 2m
    openWorkload: true # (can add additional threads when throughput is not met)
    minThreads: 3 # requires open workload
    maxThreads: 15 # requires open workload
throughput:
    thinkTime: 500ms
    phases:
    -   # rampup
        duration: 10s
        tps: 10
    -   
        duration: 2m
        from: 10
        to: 20
    -   
        duration: 3m
        from: 20
        to: 30
    -   # rampdown
        duration: 20s
        from: 30
        to: 0
exitConditions:
 - passed if PeriodMax > 10 for 10 seconds
 - passed if Min < 2 for 10 minutes
 - failed if Mean > 300 for 10 periods
 - failed if PeriodMax > 20 for 3 periods
";

        #endregion
        [Fact]
        public void WhenConfigurationStringIsParsed_ThenConfigIsDeserialised()
        {
            var parsed = YamlHelper.Deserialize<Config>(AllConfigBasic);
            parsed = ConfigHelper.Build(parsed, null);
            parsed.Should().NotBeNull();
            parsed.Throughput.Phases.Should().HaveCount(3);
            parsed.Throughput.RampUp.Should().BeGreaterThan(TimeSpan.Zero);
            parsed.Throughput.HoldFor.Should().BeGreaterThan(TimeSpan.Zero);
            parsed.Throughput.RampDown.Should().BeGreaterThan(TimeSpan.Zero);
            parsed.Throughput.ThinkTime.Should().BeGreaterThan(TimeSpan.Zero);
            parsed.Concurrency.RampUp.Should().BeGreaterThan(TimeSpan.Zero);
            parsed.Concurrency.HoldFor.Should().BeGreaterThan(TimeSpan.Zero);
            parsed.Concurrency.RampDown.Should().BeGreaterThan(TimeSpan.Zero);
        }

        [Fact]
        public void WhenConfigurationStringIsParsed_ThenConfigIsDeserialisedPhases()
        {
            var parsed = YamlHelper.Deserialize<Config>(AllConfigPhases);
            parsed = ConfigHelper.Build(parsed, null);
            parsed.Should().NotBeNull();
            parsed.Throughput.Phases.Should().HaveCount(4);
            parsed.Concurrency.RampUp.Should().BeGreaterThan(TimeSpan.Zero);
            parsed.Concurrency.RampDown.Should().BeGreaterThan(TimeSpan.Zero);
        }
    }
}

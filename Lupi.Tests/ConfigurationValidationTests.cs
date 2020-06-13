using FluentAssertions;
using System;
using Xunit;
using Lupi.Configuration;
using System.Threading.Tasks;
using System.Linq;

namespace Lupi.Tests
{
    public class ConfigurationValidationTests
    {
        #region ConfigStrings
        private const string ConfigBasic = @"
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
        private const string ConfigPhases = @"
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
";
        private const string ConfigMinimal = @"
test:
    assemblyPath: path/to/my.dll
    testClass: MyNamespace.MyClass # optional if method name is unique in the assembly
    testMethod: MyMethod
concurrency:
    threads: 10 
throughput:
    thinkTime: 1s
";

        private const string InvalidNegativeValues = @"
test:
    assemblyPath: path/to/my.dll
    singleTestClassInstance: true
    testClass: MyNamespace.MyClass # optional if method name is unique in the assembly
    testMethod: MyMethod
concurrency:
    threads: -4
    openWorkload: true # (can add additional threads when throughput is not met)
    minThreads: -3 # requires open workload
    maxThreads: -15 # requires open workload
throughput:
    tps: -20
    holdFor: 10m
    thinkTime: 500ms
";
        private const string InvalidEmpty = @"
test:
    assemblyPath: path/to/my.dll
";

        #endregion
        [Theory]
        [InlineData(ConfigBasic)]
        [InlineData(ConfigPhases)]
        [InlineData(ConfigMinimal)]
        public async Task WhenBasicConfigurationIsValidated_ThenValidationPasses(string config)
        {
            var validator = new ConfigurationValidator();
            var parsed = await ConfigHelper.GetConfigFromString(config);
            var result = validator.Validate(parsed);
            result.Errors.Should().BeEmpty();
            result.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData(InvalidNegativeValues)]
        [InlineData(InvalidEmpty)]
        public async Task WhenBasicConfigurationIsValidated_ThenValidationFails(string config)
        {
            var validator = new ConfigurationValidator();
            var parsed = await ConfigHelper.GetConfigFromString(config);
            var result = validator.Validate(parsed);
            result.Errors.Should().NotBeEmpty();
            result.IsValid.Should().BeFalse();
        }
    }
}

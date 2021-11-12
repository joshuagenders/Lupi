using FluentAssertions;
using Xunit;
using Lupi.Configuration;
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
    testClass: MyNamespace.MyClass
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
    openWorkload: true
    minThreads: 3
    maxThreads: 15
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
    testClass: MyNamespace.MyClass
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
    openWorkload: true
    minThreads: 3
    maxThreads: 15
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
    holdFor: 2m
throughput:
    thinkTime: 1s
";

        private const string InvalidNegativeValues = @"
test:
    assemblyPath: path/to/my.dll
    singleTestClassInstance: true
    testClass: MyNamespace.MyClass
    testMethod: MyMethod
concurrency:
    threads: -4
    openWorkload: true
    minThreads: -3
    maxThreads: -15
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
        public void WhenBasicConfigurationIsValidated_ThenValidationPasses(string config)
        {
            var validator = new ConfigurationValidator();
            var parsed = YamlHelper.Deserialize<Config>(config);

            if (!parsed.Concurrency.Phases.Any())
                parsed.Concurrency.Phases = parsed.BuildStandardConcurrencyPhases();

            if (!parsed.Throughput.Phases.Any())
                parsed.Throughput.Phases = parsed.BuildStandardThroughputPhases();

            var result = validator.Validate(parsed);
            result.Errors.Should().BeEmpty();
            result.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData(InvalidNegativeValues)]
        [InlineData(InvalidEmpty)]
        public void WhenBasicConfigurationIsValidated_ThenValidationFails(string config)
        {
            var validator = new ConfigurationValidator();
            var parsed = YamlHelper.Deserialize<Config>(config);
            var result = validator.Validate(parsed);
            result.Errors.Should().NotBeEmpty();
            result.IsValid.Should().BeFalse();
        }
    }
}

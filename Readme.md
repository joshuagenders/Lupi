<img
    alt="Lupi"
    src="https://github.com/joshuagenders/lupi/blob/master/Logo.png"
    width="200"
/>

# Lupi
Lupi is a load testing framework written for the dotnet runtime.

Lupi supports a plugin system for loading and executing code from compatible DLLs.

# Who this is for
* You want to write a load test using code but don't want to have to write a lot of code to control the load profile.
* You want to write load tests in a dotnet language.
* Don't need local visualisations of test execution, just a reliable load agent.

## Quickstart
### Pre-requisites
* [.NET Core SDK](https://dotnet.microsoft.com/download)

### Examples
See the [Examples here](https://github.com/joshuagenders/Lupi/tree/master/Lupi.Examples)

### Publish test solution
Lupi uses a plugin architecture. Start by writing a test and then publish your test solution.
```bash
dotnet publish -c Release
```
### Create configuration file
Create a configuration file. Here's a simple example - the full configuration specification is found further below.
```yaml
test:
    assemblyPath: path/to/my.dll
    testClass: MyNamespace.MyClass
    testMethod: MyMethod
concurrency:
    threads: 10 
    rampUp: 10s
    holdFor: 2m
throughput:
    thinkTime: 1s500ms
```

### Run Lupi
```bash
dotnet run --project Lupi/Lupi.csproj /path/to/myConfigFile.yml
```

## Configuration
```yaml
test:
    assemblyPath: path/to/my.dll # relative to the the configuration file or full path
    singleTestClassInstance: true
    testClass: MyNamespace.MyClass
    testMethod: MyMethod
    setupClass: MyNamespace.SetupClass
    setupMethod: Init # executed once before test method execution
    teardownClass: MyNamespace.TeardownClass
    teardownMethod: Teardown # executed once at the end of the test
throughput:
    thinkTime: 1s500ms
    tps: 20 # tests per second
    rampUp: 20s
    holdFor: 10m
    rampDown: 2m
    # mutually exclusive to other throughput parameters.
    # do not provide both phases AND tps, rampUp, holdFor or rampDown, as phases are generated from them when provided.
    phases:
    -   # rampup
        duration: 2m
        from: 10
        to: 20
    -
        duration: 10s
        tps: 20
    -   # rampdown
        duration: 20s
        from: 20
        to: 0
concurrency:
    threads: 10 
    rampUp: 10s
    holdFor: 2m10s
    rampDown: 10s
    openWorkload: true # i.e. can add additional threads when throughput is not met
    minThreads: 3 # requires open workload
    maxThreads: 1500 # requires open workload
    threadIdleKillTime: 5s # idle threads will exit if idle for too long. requires open workload.
    # phases are also supported for concurrency
    phases:
    -
        duration: 2m
        threads: 20
    -   duration: 30s
        from: 20
        to: 15
listeners:
    activeListeners:
    - file
    - statsd
    - console
    file:
        path: results.log
        format: {FinishedTime:dd/MM/yy H:mm:ss zzz};{Passed,5};{Duration}
    console:
        format: {FinishedTime:dd/MM/yy H:mm:ss zzz} - Passed: {Passed,5} - Duration: {Duration}
    statsd:
        host: 10.0.0.1
        port: 8125
        prefix: my.prefix
        bucket: my.test.bucket
engine:
    resultPublishingInterval: 250ms # how often the result publishing handlers are invoked
    checkInterval: 150ms # how often thread levels / throughput is assessed
    aggregationInterval: 2s # how often results are aggregated
baseConfig: BaseConfig.yml
```

# Concepts
## Concurrency and Throughput
Throughput (the number of requests) and concurrency (the number of possible concurrent test executions) are separate concepts in Lupi. Each can be ramped up or down independently of each other (though lowering concurrency may restrict the ability to meet desired throughput).

## Phases
Concurrency and throughput in Lupi tests are divided into stages called phases.
Each phase executes in order.

When you specify ramp up, holdFor and ramp down values, Lupi generates a phase for each at run time - known as standard phases.

### Standard phases
```yaml
concurrency:
    threads: 10
    rampUp: 10s
    holdFor: 2m30s
    rampDown: 20s
throughput:
    tps: 200
    rampUp: 30s
    holdFor: 2m
    rampDown: 30s
```

### Custom phases
Phases can be constant, or a linear progression from one value to another.
```yaml
concurrency:
    -
        duration: 2m30s
        threads: 20
throughput:
    -   # rampup
        duration: 2m
        from: 10
        to: 20
    -
        duration: 10s
        tps: 20
    -   # rampdown
        duration: 20s
        from: 20
        to: 0
```

which is equivalent to

```yaml
concurrency:
    threads: 20
throughput:
    -   # rampup
        duration: 2m
        from: 10
        to: 20
    -
        duration: 10s
        tps: 20
    -   # rampdown
        duration: 20s
        from: 20
        to: 0
```

## Reporting test results
`TimeSpan` objects returned from test methods will be used as the duration value in test results.
Any value type will be serialised with `toString()`, and other types will be JSON serialised.
Exceptions are also JSON serialised.

## Listeners
Listeners are used to process the results of tests.
The provided listeners are:
### File
On each test result, the file listener writes the `TestResult` to file. By default the format is JSON.
The `Format` configuration parameter is a string.Format string that uses variable names instead of integer indexes of an array.
Availble fields are:
- string ThreadName
- bool Passed
- string Result
- TimeSpan Duration
- DateTime FinishedTime

E.g.
`{FinishedTime:dd/MM/yy H:mm:ss zzz} - {Passed,5} - {Duration}`

### Statsd
The statsd listener sends test metrics to the configured statsd host.
metrics are prefixed with the configured prefix and bucket parameters.

Timers:
- success
- failure

Guages:
- threads

Counters (Lupi internals):
- taskstart
- taskcomplete
- requesttaskexecutionstart
- requesttaskexecutionend
- taskkillrequested
- taskkill
- diedofboredom

### Console
The console listener writes results to the console output.
It also takes a named format string.
Available fields are:
- double Min
- double Max
- double MovingAverage
- double PeriodMin
- double PeriodMax
- double PeriodAverage
- double PeriodLength
- int Count
- int PeriodErrorCount
- int PeriodSuccessCount

## Open Workload
Whenever throughput is specified, an open workload is created. Specifying concurrency values along with throughput will create threads as desired, but they will wait until they are allowed to execute.

If `concurrency.openWorkload` is `true`, then the concurrency phases are ignored and Lupi will try and allocate as many threads as it needs to in order to reach desired tests per second, within the `concurrency.minThreads` and `concurrency.maxThreads` limits.

When concurrency phases are provided, then the number of threads is determined by the phases, and threads will wait until they are permitted to execute.
In both scenarios, setting thread levels too low will result in a closed workload as new thread allocation will not be possible.

## Dependency Injection
Lupi will attempt to find and invoke a method in the provided test assembly that returns a `Microsoft.Extensions.DependencyInjection.IServiceProvider`.
The method must be defined as static or the owning class must have a default constructor.

E.g.

```csharp
public static IServiceProvider BuildServiceProvider() =>
    new ServiceCollection()
        .AddTransient<IInternalDependency, InternalDependency>()
        .BuildServiceProvider();

```

# License
Lupi is licensed under the [MIT license](https://github.com/joshuagenders/Lupi/blob/master/LICENSE).

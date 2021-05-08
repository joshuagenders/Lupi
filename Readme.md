<img
    alt="Lupi"
    src="https://github.com/joshuagenders/lupi/blob/main/Logo.png"
    width="200"
/>

# Lupi
Lupi is a load testing framework written for the dotnet runtime.

Lupi supports a plugin system for loading and executing code from compatible DLLs.

### Who this is for
* You want to write a load test using code but don't want to have to write a lot of code to control the load profile.
* You want to write load tests in a dotnet language.
* Don't need local visualisations of test execution, just a reliable load agent.

## Examples
See the [Examples here](https://github.com/joshuagenders/Lupi/tree/main/Lupi.Examples)

## Quickstart
<details>
  <summary>Read more</summary>

### Pre-requisites
* [.NET 5 SDK](https://dotnet.microsoft.com/download)

### Create a test solution (skip if using an existing solution)
Lupi uses a plugin architecture. Start by writing a test and then publish your test solution.

```bash
dotnet new sln
dotnet new classlib -o TestLibrary
dotnet sln add TestLibrary/TestLibrary.csproj
echo "
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace TestLibrary
{
    public class PerformanceTest
    {
        public async Task Get(CancellationToken ct)
        {
            var result = await new HttpClient().GetAsync(\"https://<website>.com/\", ct);
            result.EnsureSuccessStatusCode();
        }
    }
}

" >> TestLibrary/PerformanceTest.cs
```

### Publish test solution
```bash
dotnet publish -c Release
```

### Create configuration file
Create a configuration file. Here's a simple example - the full configuration specification is found further below.
```yaml
test:
    assemblyPath: path/to/my.dll
    testClass: MyNamespace.MyClass # e.g. TestLibrary.PerformanceTest
    testMethod: MyMethod # e.g. Get
concurrency:
    threads: 10 
    rampUp: 10s
    holdFor: 2m
throughput:
    thinkTime: 1s500ms
```
</details>

## How to run
<details>
  <summary>Read more</summary>

### Run Lupi from source
```bash
dotnet run --project Lupi/Lupi.csproj /path/to/myConfigFile.yml
```

### Run Lupi with Docker
Assuming `test_config.yaml` is in the current working directory.

#### Git Bash (Windows)
```bash
MSYS_NO_PATHCONV=1 docker run --rm --name lupi -it -v `pwd -W`:/usr/src/project joshuagenders/lupi:slim-latest /usr/src/project/test_config.yaml
```

#### CMD (Windows)
```bash
docker run --rm --name lupi -it -v %cd%:/usr/src/project joshuagenders/lupi:slim-latest /usr/src/project/test_config.yaml
```

### Images
There are [two images available](https://hub.docker.com/r/joshuagenders/lupi) for Lupi. 

The `latest` tag is based on [microsoft-playwright](https://hub.docker.com/_/microsoft-playwright) and should only be used when a headless browser is required as a test dependency.

The other image is `slim-latest` which is recommended for most use cases, and is based on `mcr.microsoft.com/dotnet/runtime`.

</details>

## Configuration

<details>
  <summary>Read more</summary>

```yaml
test:
    assemblyPath: path/to/my.dll # relative to the the configuration file or full path
    singleTestClassInstance: true
    testClass: MyNamespace.MyClass
    testMethod: MyMethod # for overridden methods, will select the method with the least parameters
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
engine:
    resultPublishingInterval: 250ms # how often the result publishing handlers are invoked
    checkInterval: 150ms # how often thread levels / throughput is assessed
    aggregationInterval: 2s # how often results are aggregated, relies on the results being published (so ensure is greater than resultPublishingInterval)
exitConditions:
 - failed if PeriodAverage > 150 for 10 periods
 - passed if Min < 30.42 for 10 seconds
 - failed if Mean >= 600 for 10 minutes

baseConfig: BaseConfig.yml
```

### Environment Variables
Environment variables values can be interpolated into configuration files using the `${variable_name}` syntax.

E.g.

```yaml
listeners:
    statsd:
        host: ${STATSD_HOST}
```

### Base Config
When a `baseConfiguration` file is specified (relative to the configuration file, or the full path) then the config is loaded and merged.

Base configurations can also have their own base configurations; base configurations will be loaded until the property is blank or a circular reference is found.

### Logging
Lupi uses [Serilog](https://github.com/serilog/serilog) for logging. The available sinks are `File` and `Console`.

Logging can be configured through the [appsettings.json](Lupi/appsettings.json) file.

Also see [Serilog's configuration documentation](https://github.com/serilog/serilog-settings-configuration).


</details>

## Concepts

<details>
  <summary>Read more</summary>

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
Phases can be constant (tests per second `tps`), or a linear progression `from` one value `to` another.
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

## Open Workload
Whenever throughput is specified Lupi uses an open workload. Specifying concurrency values along with throughput will create threads as desired, but they will wait until they are allowed to execute.

If `concurrency.openWorkload` is `true`, then the concurrency phases are ignored and Lupi will try and allocate as many threads as it needs to in order to reach desired tests per second, within the `concurrency.minThreads` and `concurrency.maxThreads` limits.

When concurrency phases are provided, then the number of threads is determined by the phases, and threads will wait until they are permitted to execute.
In both scenarios, setting thread levels too low will result in a closed workload as new thread allocation will not be possible.

## Reporting test results
The `Result`, `Duration` and `Passed` properties of a test result can be set by returning values from the test method.
The values are mapped based on return type:
- `Result` - return a `System.String`
- `Duration` - return a `System.TimeSpan`
- `Passed` - return a `System.Bool`

When an exception is raised or returned the test result is marked as failed.
If the object returned matches none of the above, then value types (excluding `bool`) will be serialised with `toString()`, and other types will be JSON serialised.

Exceptions are also JSON serialised into the `Result` property.

## Listeners
Listeners are used to process the results of tests.
The provided listeners are:

### File
On each test result, the file listener writes the `TestResult` to file. By default the format is JSON.
The `Format` configuration parameter is a `string.Format` string that uses variable names instead of integer indexes of an array.
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
metrics are prefixed with the configured prefix parameter.

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

The `Format` configuration parameter is a `string.Format` string that uses variable names instead of integer indexes of an array.

Available fields are:

`Lupi.Listeners.AggregatedResult`

- double Mean (ms)
- double StandardDeviation
- int Count
- double MovingAverage (ms)
- double Min (ms)
- double Max (ms)
- double PeriodMin (ms)
- double PeriodMax (ms)
- double PeriodAverage (ms)
- int PeriodErrorCount
- int PeriodSuccessCount

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

## Exit Conditions
Exit conditions are assessed in each aggregation period and the test exits with passed (`0`) or failed(`1`) return code.
The format is:
`<PassedFailed> if <Property> <operator> <value> for <period> <periodType>`

Valid property names are the same properties available in the console listener (`Lupi.Listeners.AggregatedResult`).

Valid operators are `<`, `>`, `>=`, `<=` and `=`.

Timing values (e.g. min, max) are in milliseconds.

Valid periodTypes are `seconds`, `minutes`, `periods`.

E.g.

```
failed if PeriodAverage > 150 for 10 periods
passed if Min < 30.42 for 10 seconds
failed if Mean >= 600 for 10 minutes
```
</details>

## License
Lupi is licensed under the [MIT license](https://github.com/joshuagenders/Lupi/blob/main/LICENSE).

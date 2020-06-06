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
    waitTime: 1s500ms
```

### Run Lupi
```bash
dotnet run --project Lupi/Lupi.csproj /path/to/myConfigFile.yml
```

## Configuration
```yaml
test:
    assemblyPath: path/to/my.dll
    singleTestClassInstance: true
    testClass: MyNamespace.MyClass
    testMethod: MyMethod
    setupClass: MyNamespace.SetupClass
    setupMethod: Init
    teardownClass: MyNamespace.TeardownClass
    teardownMethod: Teardown
throughput:
    waitTime: 1s500ms
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
    minConcurrency: 3 # requires open workload
    maxConcurrency: 1500 # requires open workload
listeners:
    activeListeners:
    - file
    - statsd
    - console
    file:
        path: results.log
    statsd:
        host: 10.0.0.1
        port: 8125
        prefix: my.prefix
        bucket: my.test.bucket
engine:
    resultPublishingInterval: 250ms # how often the result publishing handlers are invoked
    checkInterval: 150ms # how often thread levels / throughput is assessed
baseConfig: BaseConfig.yml
```

# Concurrency and Throughput
Throughput (the number of requests) and concurrency (the number of possible concurrent test executions) are separate concepts in Lupi. Each can be ramped up or down independently of eachother (though lowering concurrency may restrict the ability to meet desired throughput).

# Specifying load
There are two ways to specify load for concurrency and throughput. The first is static values with optional ramp-up / ramp-down periods. 

For example:
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

The second is to specify a series of phases. Phases can be constant, or a linear progression from one value to another.
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
    concurrency
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

## Returning test results
`TimeSpan` objects returned from test methods will be used as the duration value in test results.
Any value type will be serialised with `toString()`, and other types will be JSON serialised.
Exceptions are also JSON serialised.

## Open Workload
Whenever throughput is specified, an open workload is created. Threads will wait and execute as fast as possible.

If `concurrency.openWorkload` is `true`, then the concurrency phases are ignored and Lupi will try and allocate as many threads as it needs to in order to reach desired tests per second, within the `concurrency.minThreads` and `concurrency.maxThreads` limits.

When concurrency phases are provided, then the number of threads is determined by the phases, and threads will wait until they are permitted to execute.
In both scenarios, setting thread levels too low will result in a closed workload as new thread allocation will not be possible.

# Examples
See the [Examples here](https://github.com/joshuagenders/Lupi/tree/master/Lupi.Examples)

# License
Lupi is licensed under the [MIT license](https://github.com/joshuagenders/Lupi/blob/master/LICENSE).

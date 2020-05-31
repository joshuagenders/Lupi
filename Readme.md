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
```bash
dotnet publish -c Release
cd Lupi/bin/Release/netcoreapp3.0/publish
dotnet run Lupi.dll <config path>
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
concurrency:
    threads: 10 
    rampUp: 2m
    openWorkload: true # i.e. can add additional threads when throughput is not met
    minConcurrency: 3 # requires open workload
    maxConcurrency: 1500 # requires open workload
throughput:
    tps: 20
    rampUp: 20s
    holdFor: 10m
    rampDown: 2m
    phases:  # mutually exclusive to other throughput parameters. do not provide both phases AND tps, rampUp, holdFor or rampDown
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
    tokenGenerationInterval: 100ms
    resultPublishingInterval: 250ms
    threadAllocationInterval: 150ms
baseConfig: BaseConfig.yml
```

# Concurrency and Throughput
Throughput (the number of requests) and concurrency (the number of possible concurrent test executions) are separate concepts in Lupi. Each can be ramped up or down independently of eachother (though lowering concurrency may restrict the ability to meet desired throughput).

# Specifying load
There are two ways to specify load for concurrency and throughput. The first is static values with optional ramp-up / ramp-down periods. 

`throughput.holdFor` is used as the hold for value for both concurrency and throughput.

For example:
```yaml
concurrency:
    threads: 10
    rampUp: 10s
    rampDown: 10s
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
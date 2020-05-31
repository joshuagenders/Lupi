![Lupi](https://github.com/joshuagenders/lupi/blob/master/Claw_Marks.png?raw=true)
# Lupi
Lupi is a load testing framework written for the dotnet runtime.

Lupi supports a plugin system for loading and executing code from compatible DLLs.

## Quickstart
`dotnet run config.yaml`

## Configuration
```yaml
test:
    assemblyPath: path/to/my.dll
    singleTestClassInstance: true
    testClass: MyNamespace.MyClass
    testMethod: MyMethod
    assemblySetupClass: MyNamespace.SetupClass
    assemblySetupMethod: Init
    assemblyTeardownClass: MyNamespace.TeardownClass
    assemblyTeardownMethod: Teardown
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
```

# Who this is for
* You want to write a load test using code but don't want to have to write a lot of code to control the load profile.
* You want to write load tests in a dotnet language.
* Don't need local visualisations of test execution, just a reliable load agent.

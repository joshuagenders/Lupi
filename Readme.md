# Yakka
Yakka is a load testing framework written for the dotnet runtime.

Yakka supports a plugin system for loading and executing code from compatible DLLs.

## Quickstart
`dotnet run config.yaml`

## Configuration
```yaml
test:
    assemblyPath: path/to/my.dll
    singleTestClassInstance: true
    testClass: MyNamespace.MyClass # optional if method name is unique in the assembly
    testMethod: MyMethod
    assemblySetupClass: MyNamespace.SetupClass
    assemblySetupMethod: Init
    assemblyTeardownClass: MyNamespace.TeardownClass
    assemblyTeardownMethod: Teardown
concurrency:
    threads: 10 
    rampUp: 2m
    openWorkload: true # (can add additional threads when throughput is not met)
    maxConcurrency: 1500 # requires open workload
throughput:
    tps: 20
    rampUp: 20s
    holdFor: 10m
    rampDown: 2m
    phases:
    -   # rampup
        duration: 10s
        tps: 10
    -   
        duration: 2m
        from: 10
        to: 20
    -   # rampdown
        duration: 20s
        from: 20
        to: 0
```

# Who this is for
* You want to write a load test using code but don't want to have to write a lot of code to control the load profile.
* You want to write load tests in a dotnet language.

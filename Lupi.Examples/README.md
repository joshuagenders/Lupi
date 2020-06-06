# Lupi Examples
Examples use [Puppeteer Sharp](https://github.com/hardkoded/puppeteer-sharp) and the [.NET HTTP client](https://docs.microsoft.com/en-us/dotnet/api/system.net.http.httpclient?view=netcore-3.1). The browser and Http Client are both accessed through the `GlobalState` class, where the setup, teardown and dependency injection methods are defined.

# Publish
`dotnet publish -c Release`

# Running the tests
Update `test.assemblyPath` in the configuration file(s) to match the relative path from the current working directory, then execute Lupi.

E.g.
`dotnet run --project ../Lupi/Lupi.csproj ./Configurations/OpenWorkload.yml`

# Common use cases
## I want to run as much load as I can
 > Specify an extremely high throughput with an open workload

```yaml
concurrency:
    openWorkload: true
    minThreads: 10
    maxThreads: 1000
throughput:
    tps: 5000
    holdFor: 2m
```

 > OR Specify a high number of threads with no throughput control or think time

```yaml
concurrency:
    threads: 250
    holdFor: 2m
```
## I care both about the number of _concurrent_ tests and the _rate_ at which tests are executed

> i.e. I want to run tests at a specific rate, run this many threads, it should be enough

```yaml
concurrency:
    threads: 20
    holdFor: 5m
throughput:
    tps: 15.2
    holdFor: 5m
```

## I want to run tests at a specific rate, run as many threads as necessary
```yaml
concurrency:
    openWorkload: true
    minThreads: 2
    maxThreads: 300
throughput:
    tps: 15.2
    holdFor: 5m
```

## I want to run tests at a specific rate, with a closed workload

> E.g. A system that is normally invoked by a set of single-threaded workers.

```yaml
concurrency:
    threads: 20
    holdFor: 1m
throughput:
    thinkTime: 150ms
    holdFor: 1m
```

> OR throughput with a `concurrency.maxThreads` low enough that the thread count remains at max for the given throughput.

```yaml
concurrency:
    maxThreads: 20
    minThreads: 15
throughput:
    tps: 1000
    holdFor: 1m
```

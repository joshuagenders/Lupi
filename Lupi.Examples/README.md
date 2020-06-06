# Lupi Examples
Examples use [Puppeteer Sharp](https://github.com/hardkoded/puppeteer-sharp) and the [.NET HTTP client](https://docs.microsoft.com/en-us/dotnet/api/system.net.http.httpclient?view=netcore-3.1). The browser and Http Client are both accessed through the `GlobalState` class, where the setup, teardown and dependency injection methods are defined.

# Publish
`dotnet publish -c Release`

# Running the tests
Update `test.assemblyPath` in the configuration file(s) to match the relative path from the current working directory, then execute Lupi.

E.g.
`dotnet run --project ../Lupi/Lupi.csproj ./Configurations/OpenWorkload.yml`

# Common use cases
> I want to run as much load as I can
 - Specify an extremely high throughput with an open workload

 OR
 
 - Specify a high number of threads with no throughput control or think time

> I care both about the number of _concurrent_ tests and the _rate_ at which tests are executed

i.e. I want to run tests at a specific rate, run this many threads, it should be enough
 - specific concurrency and throughput patterns

> I want to run tests at a specific rate, run as many threads as necessary

 - throughput with open workload

> I want to run tests at a specific rate, with a closed workload.

E.g. A system that is normally invoked by a set of single-threaded workers.
 - concurrency only with think time
 - throughput with a `concurrency.maxThreads` low enough that the thread count remains at max for the given throughput.

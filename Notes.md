# Remaining MVP
- add more unit tests
- add holdFor for concurrency  
- improve test result metadata
- Add thread count, tokens, kill tokens etc. to output of statsd listener
- Test actual examples against real site
- Aggregations with basic console reporting
- profile the load agent and address hotspots, generate flame graphs
- Verbose logging
- Update docs and examples as required
- Publish repo and nuget packages

# Next Features
## Refactor thread control 
move execution all onto main thread

## Configuration inheritance
specify a base config - has bugs - test + fix
-write own mapper

## Aggregating listener
- what are the requirements / use cases here?
  - console listener

## Make console reporter report better
- use aggregator results
- Configurable file listener output formats

## Config validation
Required fields

---

## Returning pass/fail status from tests
 - Requires aggregator listener
 - Use exceptions or use Tuple<bool, string>?

## Exit Conditions
- Requires aggregator listener
- Failure rate/percentage
- Failure count
- Latency over x average / max in y period

## Other
- Write blog post(s?)

## Maybe - Docker image
- Create docker compose / container image
- Publish image

## Maybe - "Requests" scenario generation
- Browser, browser emulator and http client options
- Think about how to manage scope and what to support
- Look at roslyn compilation
- Look at all jmeter node types + what youd typically use beanshell for
- Plus what you'd normally write in locust
  - Things like when to clear/set cookies
  - Actions to perform on screen
- Maybe allow js scripts and assertions through puppeteer
- Is there a nice page object pattern in yaml? is that taurus 'scenarios'

## Maybe - Datasources
Thread-safe implementations of datasources, making scope access and lifetime obvious
- HTTP
- file
  - CSV
  - JSON
  - raw line reader
Parsed and passed into test method

## Maybe - Gherkin syntax support - requests scenario
- Because I can
- Nuget?

```gherkin
Given the default address http://blazedemo.com
And header Accept = Application/Json
When 10 users arrive
At 0 to 20 requests per second
Over 30 seconds
And then 10 users
At 20 requests per second hold
Then the average response time is 200ms
```

```gherkin
Given the default address http://blazedemo.com
When users request 0.5 to 0.1 requests per second over 1 minute
And Then I request 1 to 20 times per second for for 1 minute
And Then I request 20 times per second for for 5 minutes
Then the failure rate is below 2 percent
```

```gherkin
Given the default address http://blazedemo.com
And header Accept = Application/Json
And think time of 1 second
When 10 users arrive over 30 seconds
And then 10 users hold for 5 minutes
Then the average response time is 200ms
```

## Maybe - Attribute Framework
- Test attributes
- Method attribute filtering
- Setup/teardown attributes
- Datasource input attributes
- Scoping/Threading attributes

## Maybe - Website for the tool
Static site generator
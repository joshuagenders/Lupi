# Remaining MVP
- add more unit tests
  - failed tests / exception handling
  - threads that hold on for too long/lock
  - separate values for concurrency + throughput rampup/down
  - custom phases
  - result aggregation
  - only concurrency phases
  - only throughput phases

- Add configurable + verbose logging
- profile the load agent and address hotspots, generate flame graphs
- complete grafana dashboard
- add grafana graphs pictures to readme + examples readme
- Test actual examples against real site
- Update docs and examples as required
  - make quickstart easier to follow, including how to publish properly
- Publish repo and nuget packages

# Next Features
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

```gherkin
Given the site http://blazedemo.com
And header Accept = Application/Json
When 10 users arrive
At 0 to 20 requests per second
Over 30 seconds
And then 10 users
At 20 requests per second hold
Then the average response time is 200ms
```

```gherkin
Given the site http://blazedemo.com
When users request 0.5 to 0.1 requests per second over 1 minute
And Then I request 1 to 20 times per second for for 1 minute
And Then I request 20 times per second for for 5 minutes
Then the failure rate is below 2 percent
```

```gherkin
Given the api http://blazedemo.com/api/examples?value={value}
And header Accept = Application/Json
And think time of 1 second
And value is one of 1,2,3,4,5 # a valid date, a valid date after now, between 2 and 40 etc. come up with useful ways to describe data -> plug in faker
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

## Maybe - interpreter or wizard

## Maybe - filters for statsd metrics sent (inclusive+exclusive)
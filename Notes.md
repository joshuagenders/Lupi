# Next Features

# Config validation
required fields

## Unit tests
rampdown threads
rampdown rps
min/max threads open workload
test results publishing
number of class instances created

# Improve docs
What fields are required when
threads controlled separately to throughput
can use threads with think time or throughput
link to examples
graphics

# Refactor thread control - separate concerns

# Make console reporter report on bucket average - (10 results at a time)?

# Aggregating listener
 - Aggregate results
   - what are the requirements / use cases here?

# Returning pass/fail status from tests
 - requires aggregator listener
 - use exceptions or use Tuple<bool, string>?

# Exit Conditions
- requires aggregator listener
- failure rate/percentage
- failure count
- latency over x average / max in y period

# todo
 - features above
 - more unit testing
 - test with local grafana
 - test actual examples
 - update docs and examples as required
 - review licenses
 - publish repo and nuget packages
 - write blog post(s?)

# Maybe - Docker image
- create docker compose / container image
- publish image

# Maybe - "Requests" scenario generation
- browser, browser emulator and http client options
- think about how to manage scope and what to support
- look at roslyn compilation
- look at all jmeter node types + what youd typically use beanshell for
- plus what you'd normally write in locust
  - things like when to clear/set cookies
  - actions to perform on screen
- maybe allow js scripts and assertions through puppeteer
- is there a nice page object pattern in yaml? is that taurus 'scenarios'

# Maybe - Datasources
Thread-safe implementations of datasources, making scope access and lifetime obvious
 - http
 - file
   - csv
   - json
   - raw line reader
Parsed and passed into test method

# Maybe - Gherkin syntax support - requests scenario
 - because I can
 - nuget?

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

# Maybe - Attribute Framework
test attributes
method attribute filtering
setup/teardown attributes
datasource input attributes
scoping/threading attributes

# maybe - website
static site generator
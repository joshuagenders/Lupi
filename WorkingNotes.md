# Remaining MVP
- implement exit conditions for avg response time, error count, error rate, std-dev
- return pass/fail status from test (true/false, tuple(T,U) where T : bool, tuple(T,U,V) where T : bool, U : timespan)?
- add validation for plugin (class/method/assembly not exists)
- add validation+setup+teardown start/stop stages etc. to console output
- refactor and cleanup
- complete grafana dashboard
- add better grafana graphs + pictures to readme/examples readme
- Test actual examples against real site
- Update docs and examples as required
- Publish nuget packages + docker images

# Next Features
## Returning pass/fail status from tests
 - Use exceptions or use Tuple<bool, string>?

## Exit Conditions
- Requires aggregator listener - or process output file
- Failure rate/percentage
- Failure count
- test duration over/under x average / max in y period
pass/fail

## Other
- Write blog post(s?)
- Validate implementation with peers
- Add to Taurus

## Docker image
- Create docker compose / container image
- Publish image

--------
# Maybes in order of likelihood
## Maybe - filters for statsd metrics sent (inclusive+exclusive)

## Maybe - Website for the tool
Static site generator

## Maybe - add returned string count to exit conditions
- think about checking returned strings from methods for exit condition (what is the use case - particular error codes?)
  - would need to keep track of matches


## Maybe - Datasources
Thread-safe implementations of datasources, making scope access and lifetime obvious
- HTTP
- file
  - CSV
  - JSON
  - raw line reader
Parsed and passed into test method

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

public GherkinDocument ParseTokenString(string statements)
{
    return new Parser().Parse(new TokenScanner(new StringReader(statements)),
                                                new TokenMatcher());
}

public List<Pickle> GetPickles(GherkinDocument document)
{
    return new Compiler().Compile(document);
}
map regex -> func
func takes parsed arguments
executes relevant step handler

## Maybe - Attribute Framework
- Test attributes
- Method attribute filtering
- Setup/teardown attributes
- Datasource input attributes
- Scoping/Threading attributes

## Maybe - interpreter or wizard CLI
- create new tests and base configurations
- if using gherkin, can make builder interface

# Next Features

## Assembly loading
plugin and DI pattern design 
  - autofac with optional builder interface
  look for a particular static method signature similar to asp.net

# Method inputs and returns
Timers, CancellationToken, registered interfaces, complex objects with default constructor
anything returned sent to listeners, including timers which override the reported ellapsed time value

# Listener support
 - Listeners automatically registered and invoked on test result
 - File listener
 - Statsd listener
 - Console listener
 - Aggregating listener (means listeners will need to be stateful)

# Logging

# Exit Conditions
failure rate/percentage
failure count

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

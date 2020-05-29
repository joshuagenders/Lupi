# Next Features

## Assembly loading
plugin and DI pattern design 
  - autofac with optional builder interface
  look for a particular static method signature similar to asp.net

# Method inputs and returns
Timers, CancellationToken, registered interfaces, complex objects with default constructor

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

# Maybe - Gherkin syntax support
 - because I can

when I GET http://blazedemo.com
with header Accept = Application/Json
and ramp up for 1 minute to 10 threads
and ramp up for 1 minute to 20 RPS
and then I hold at 20 RPS for 5 minutes
then the average response time is 200ms

# Maybe - Attribute Framework
test attributes
method attribute filtering
setup/teardown attributes
datasource input attributes
scoping/threading attributes

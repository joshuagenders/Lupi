# Next Features
## Throughput
Rampdown throughput

### Graph throughput
const(N, T) - hold constant load of N requests per second for T seconds
line(N, K, T) - linear increase load from N to K RPS during T seconds
-> yaml type converter

## Assembly loading
plugin and DI pattern design 
go through steps of how assmebly is loaded and what is supported in locating an executing methods
i.e. autofac, cancellation tokens
start with simple model -> no required framework
pass method name, use method signature as startup module
    - autofac with optional builder interface
    look for a particular static method signature similar to asp.net


# Elastic threads
threads can die and be replaced
threads can be created to meet RPS (open workload)
use producer-consumer?
task.ContinueWith
tasks.RemoveAt(Task.WaitAny(tasks.ToArray()));

# Logging

# Exit Conditions
failure rate/percentage
failure count


# Listener support
 - Listeners automatically registered and invoked on test result
 - File listener
 - Statsd listener

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

# Maybe - Attribute Framework
test attributes
method attribute filtering
setup/teardown attributes
datasource input attributes
scoping/threading attributes

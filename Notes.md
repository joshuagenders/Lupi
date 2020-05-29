# Next Features

## Assembly loading
plugin and DI pattern design 
go through steps of how assembly is loaded and what is supported in locating an executing methods
i.e. autofac, cancellation tokens
start with simple model -> no required framework
pass method name, use method signature as startup module
    - autofac with optional builder interface
    look for a particular static method signature similar to asp.net

# Listener support
 - Listeners automatically registered and invoked on test result
 - File listener
 - Statsd listener
 - Console listener
 - Prometheus?

# Elastic threads
threads can die and be replaced
threads can be created to meet RPS (open workload)
use producer-consumer?
task.ContinueWith
tasks.RemoveAt(Task.WaitAny(tasks.ToArray()));

thread allocator loops around and continuously cleans up old threads and creates new as required
instead of exiting -> wait in loop for signals to add threads up to max threads
todo -> a thread with a callback when it completes to update its status so it can be removed from thread array
maybe adds an I'm finished to a concurrent queue for the thread allocator to process
also todo, change thread list to concurrentbag or dict

OR
 detect when rps behind (tokens in semaphore growing, make tasks await thread control when complete so keep track of count (diff shouldn't grow))
 have a helper thread array with a max size
can span a test thread with a max iteration count
if the thread spends more than x time awaiting execution it kills itself and signals back

OR
 run thread allocation the same as throughput tokens ->
 constant loop, adjust to current thread rate, signalling mechanism via TaskExecutionRequest to kill threads

--
How threads should behave
await execution token 
should execute?
  yes - execute
  try and take from kill queue
   success -> exit

How allocation should behave
how many threads should I have right now
 - too many then queue kill token
 - not enough then spawn

'how many threads should I have right now'
  'configured - closed' -> ramp up, hold, ramp down
  'auto-scaling - open' -> requires throughput. options:
    'detect'
    - if iteration count is behind (tps - ~1 time delta of throughput)
    - if no threads awaiting execution semaphore
    - if execution sempaphore grows for x periods (10?)
    'calculate'
    - calculate likely required thread count + delta based on moving average response times

# Logging

# Method inputs and returns
Timers, CancellationToken, ContainerBuilder

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

# Maybe - Attribute Framework
test attributes
method attribute filtering
setup/teardown attributes
datasource input attributes
scoping/threading attributes

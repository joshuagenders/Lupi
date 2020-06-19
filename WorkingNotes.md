# Remaining MVP
- add tests for exit conditions:
  - single period, multiple periods, single period duration (>engine.aggt) multiple period duration, various field types, non-existant field

- return pass/fail status from test (true/false, tuple(T,U) where T : bool, tuple(T,U,V) where T : bool, U : timespan)
- add validation for plugin (class/method/assembly not exists)
- add validation+setup+teardown start/stop stages etc. to console output
- refactor, unit testing and cleanup
  - use internal modifiers where appropriate
- add better grafana graphs + pictures to readme/examples readme
- Test actual examples against real site
- Update docs and examples as required
- Publish nuget packages + docker images
- Write blog post(s)
- Validate implementation with peers
- Add to Taurus
- Create/test docker images
- Publish images

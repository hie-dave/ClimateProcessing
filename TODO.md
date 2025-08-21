# TODO

- Add concrete usage example to README
- Replace most tests' file-based script writers with in-memory writers
- Refactor VpdCalculator to generate equations dynamically based on the units of
  the input variables
- Refactor an IScriptHeaderWriter interface out of PBSWriter for easier testing
- Revise unit tests for vpd calculator. Should we test the generated equations?
- Make CdoMergetimeScriptGenerator verbosity configurable
- The integration tests (for ScriptOrchestrator and narclim script gen.) use
  file paths in the temp directory. On some systems, this can be a directory
  which generates a PBS storage directive (e.g. /scratch/prj0/tmp), which causes
  the test to emit different output and therefore fail.
- Test the new unit conversion behaviour
  - mm d-1 -> mm
  - degC -> K
  - In general test the new func-based conversion expressions
- Test aggregation methods for all variables
- Why did we not get a test failure when generating cordex processing scripts?
  - There should have been an exception thrown because no aggregation method was
    previously defined for the relative humidity variables (e.g. hursmin)

### Cordex Dataset tests

Write tests for:

- cordex config and its validation
- cordex version realisation and its interaction with activity
- cordex dataset appends "Adjust" to variable names for bias-adjusted output

### ScriptContentGenerator (or NcoScriptGenerator)

- Merge script content generation
- Rechunk script content generation
- Cleanup script content generation

### Job Orchestration (current ScriptGenerator)

- Generate qsub commands
- Manage PBS job dependencies
- All other responsibilities offloaded to dedicated services

### Ability to calculate/estimate some variables from others

- Already do this for VPD (VpdCalculator)
- Cordex dataset requires this for relative humidity
- Some datasets may need to do this for temp from min/max temp

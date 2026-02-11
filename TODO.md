# TODO

- Refactor VpdCalculator to generate equations dynamically based on the units of
  the input variables
- Refactor an IScriptHeaderWriter interface out of PBSWriter for easier testing
- Revise unit tests for vpd calculator. Should we test the generated equations?
- Make CdoMergetimeScriptGenerator verbosity configurable
- The integration tests (for ScriptOrchestrator and narclim script gen.) use
  file paths in the temp directory. On some systems, this can be a directory
  which generates a PBS storage directive (e.g. /scratch/prj0/tmp), which causes
  the test to emit different output and therefore fail.
- Why did we not get a test failure when generating cordex processing scripts?
  - There should have been an exception thrown because no aggregation method was
    previously defined for the relative humidity variables (e.g. hursmin)
- Ensure that renamed variables are renamed in the file name as well
- Ensure that variable renaming is reflected in the job names as well
- Does StandardProcessor need to unpack data? Or do so conditionally?
- Add a test to ensure that for *all* defined ClimateVariable values, we can get
  output requirements (e.g. name, units, agg. method, etc)
- Ensure in MeanProcessor's equation file, all lines end in semicolon
- Ensure output files are renamed correctly by Narclim2Dataset
- Running CSIRO + BARPA-R not supported
- Improve unit normalisation logic:
  - Collapse whitespace
  - Normalise multiplication (e.g. m.s-1 == m s-1)
  - Normalise "per" representations (e.g. m s-1 == m/s)
  - Normalise exponents (e.g. m^-2 -> m-2)?
  - Handle some basic unicodes (e.g. °C, ℃ -> degC, μmol -> umol)
- Improve data structure used for unit synonyms. Having a collection of hashsets
  would make more sense. Having a key-array pair doesn't make sense because the
  key must be part of the array anyway
- Ensure that output files have a units attribute
- Refactor the narclim2 preprocessing. Should probably become its own distinct
  preprocessor so that we don't have this clunky passing around of
  operators/dataset.

### Cordex Dataset tests

Write tests for:

- cordex config and its validation
- cordex version realisation and its interaction with activity
- cordex dataset appends "Adjust" to variable names for bias-adjusted output

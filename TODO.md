# TODO

- Add concrete usage example to README
- Replace most tests' file-based script writers with in-memory writers
- Refactor VpdCalculator to generate equations dynamically based on the units of
  the input variables
- Refactor an IScriptHeaderWriter interface out of PBSWriter for easier testing
- Revise unit tests for vpd calculator. Should we test the generated equations?

### CDOCommandGenerator

Handle CDO command generation

- Methods like GenerateRenameOperator, GenerateUnitConversionOperators
- CDO-specific constants and configurations
- Unit test to ensure that operators all correctly written with hyphens
  (previously, remapBil had hyphen but remapCon did not)
- Make verbosity configurable

### ScriptContentGenerator

- Merge script content generation
- Rechunk script content generation
- Cleanup script content generation

### Remapping Logic

- Responsible for determining remapping, interpolation algorithm, etc

### Job Orchestration

- Generate qsub commands
- Manage PBS job dependencies

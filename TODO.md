# TODO

- Add concrete usage example to README
- Replace most tests' file-based script writers with in-memory writers
- Refactor VpdCalculator to generate equations dynamically based on the units of
  the input variables
- Refactor an IScriptHeaderWriter interface out of PBSWriter for easier testing
- Revise unit tests for vpd calculator. Should we test the generated equations?
- Make CdoMergetimeScriptGenerator verbosity configurable

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

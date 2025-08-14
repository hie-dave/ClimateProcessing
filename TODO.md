# TODO

- Add concrete usage example to README
- Eliminate file IO from most unit tests by using in-memory file writers
- Refactor VpdCalculator to generate equations dynamically based on the units of
  the input variables
- Refactor an IScriptHeaderWriter interface out of PBSWriter for easier testing
- Revise unit tests for vpd calculator. Should we test the generated equations?

### ClimateVariableManager

Handle climate variable configurations

- Current static dictionaries (daveVariables, trunkVariables)
- Methods like GetVariables, GetTargetUnits, GetAggregationMethod

### CDOCommandGenerator

Handle CDO command generation

- Methods like GenerateRenameOperator, GenerateUnitConversionOperators
- CDO-specific constants and configurations

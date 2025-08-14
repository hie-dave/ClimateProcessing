# TODO

- Add concrete usage example to README
- Eliminate file IO from most unit tests by using in-memory file writers

### ClimateVariableManager

Handle climate variable configurations

- Current static dictionaries (daveVariables, trunkVariables)
- Methods like GetVariables, GetTargetUnits, GetAggregationMethod

### VPDCalculator

Handle VPD-specific calculations and script generation

- Methods like WriteVPDEquationsAsync, GenerateVPDScript
- VPD configuration and methods could go here

### CDOCommandGenerator

Handle CDO command generation

- Methods like GenerateRenameOperator, GenerateUnitConversionOperators
- CDO-specific constants and configurations

## Additional Unit Tests

- All of the above

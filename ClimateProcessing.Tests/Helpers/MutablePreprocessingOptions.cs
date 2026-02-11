using ClimateProcessing.Models;
using ClimateProcessing.Models.Options;
using ClimateProcessing.Tests.Mocks;
using ClimateProcessing.Units;

namespace ClimateProcessing.Tests.Helpers;

public class MutablePreprocessingOptions : IPreprocessingOptions
{
    public string InputDirectory { get; set; } = "/input";

    public string OutputDirectory { get; set; } = "/output";

    public VariableInfo InputMetadata { get; set; }

    public VariableInfo TargetMetadata { get; set; }

    public TimeStep InputTimeStep { get; set; }

    public TimeStep OutputTimeStep { get; set; }

    public AggregationMethod AggregationMethod { get; set; }

    public string? GridFile { get; set; }

    public InterpolationAlgorithm RemapAlgorithm { get; set; }

    public bool Unpack { get; set; }

    public int NCpus { get; set; }

    public IClimateDataset Dataset { get; set; }

    public MutablePreprocessingOptions()
    {
        ClimateVariable variable = ClimateVariable.Temperature;
        Dataset = new DynamicMockDataset(InputDirectory);
        InputMetadata = Dataset.GetVariableInfo(variable);
        TargetMetadata = Dataset.GetVariableInfo(variable);
    }
}

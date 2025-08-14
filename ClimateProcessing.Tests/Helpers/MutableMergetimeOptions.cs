using ClimateProcessing.Models;
using ClimateProcessing.Services;
using ClimateProcessing.Tests.Mocks;
using ClimateProcessing.Units;

namespace ClimateProcessing.Tests.Helpers;

/// <summary>
/// A mutable options implementation which can be used for testing. The default
/// parameters are set to perform as few operations as possible.
/// </summary>
public class MutableMergetimeOptions : IMergetimeOptions
{
    public string InputDirectory { get; set; } = "/input";
    public string OutputFile { get; set; } = "/output";
    public VariableInfo InputMetadata { get; set; } = new VariableInfo("inputVar", "K"); // Identical in/out variables
    public VariableInfo TargetMetadata { get; set; } = new VariableInfo("inputVar", "K"); // Identical in/out variables
    public TimeStep InputTimeStep { get; set; } = new TimeStep(24); // No temporal conversion required
    public TimeStep OutputTimeStep { get; set; } = new TimeStep(24); // No temporal conversion required
    public AggregationMethod AggregationMethod { get; set; } = AggregationMethod.Mean;
    public string? GridFile { get; set; } = null; // No remap
    public InterpolationAlgorithm RemapAlgorithm { get; set; } = InterpolationAlgorithm.Conservative;
    public bool Unpack { get; set; } = false;
    public bool Compress { get; set; } = false;
    public IClimateDataset Dataset { get; set; } = new StaticMockDataset("/input");
}

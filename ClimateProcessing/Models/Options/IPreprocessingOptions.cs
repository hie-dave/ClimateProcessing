using ClimateProcessing.Units;

namespace ClimateProcessing.Models.Options;

/// <summary>
/// Interface to a class which provides options for preprocessing a variable.
/// </summary>
public interface IPreprocessingOptions
{
    /// <summary>
    /// The directory containing the input files.
    /// </summary>
    string InputDirectory { get; }

    /// <summary>
    /// The path to the output directory.
    /// </summary>
    string OutputDirectory { get; }

    /// <summary>
    /// The metadata of the input variable.
    /// </summary>
    VariableInfo InputMetadata { get; }

    /// <summary>
    /// The metadata of the target variable.
    /// </summary>
    VariableInfo TargetMetadata { get; }

    /// <summary>
    /// The time step of the input variable.
    /// </summary>
    TimeStep InputTimeStep { get; }

    /// <summary>
    /// The target time step in the output file.
    /// </summary>
    TimeStep OutputTimeStep { get; }

    /// <summary>
    /// The aggregation method to use to temporally aggregate data.
    /// </summary>
    AggregationMethod AggregationMethod { get; }

    /// <summary>
    /// The optional grid file to use for remapping.
    /// </summary>
    string? GridFile { get; }

    /// <summary>
    /// The optional interpolation algorithm to use for remapping.
    /// </summary>
    InterpolationAlgorithm RemapAlgorithm { get; }

    /// <summary>
    /// Whether to unpack the input files.
    /// </summary>
    bool Unpack { get; }

    /// <summary>
    /// The number of CPUs to use for processing.
    /// </summary>
    int NCpus { get; }

    /// <summary>
    /// The climate dataset being processed.
    /// </summary>
    IClimateDataset Dataset { get; }
}

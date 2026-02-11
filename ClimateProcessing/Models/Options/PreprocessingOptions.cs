using ClimateProcessing.Units;

namespace ClimateProcessing.Models.Options;

/// <summary>
/// Options describing how a dataset should be pre-processed.
/// </summary>
public class PreprocessingOptions : IPreprocessingOptions
{
    /// <inheritdoc /> 
    public string InputDirectory { get; private init; }

    /// <inheritdoc /> 
    public string OutputDirectory { get; private init; }

    /// <inheritdoc /> 
    public VariableInfo InputMetadata { get; private init; }

    /// <inheritdoc /> 
    public VariableInfo TargetMetadata { get; private init; }

    /// <inheritdoc /> 
    public TimeStep InputTimeStep { get; private init; }

    /// <inheritdoc /> 
    public TimeStep OutputTimeStep { get; private init; }

    /// <inheritdoc /> 
    public AggregationMethod AggregationMethod { get; private init; }

    /// <inheritdoc /> 
    public string? GridFile { get; private init; }

    /// <inheritdoc /> 
    public InterpolationAlgorithm RemapAlgorithm { get; private init; }

    /// <inheritdoc /> 
    public bool Unpack { get; private init; }

    /// <inheritdoc /> 
    public int NCpus { get; private init; }

    /// <inheritdoc /> 
    public IClimateDataset Dataset { get; private init; }

    /// <summary>
    /// Creates a new preprocessing options instance.
    /// </summary>
    /// <param name="inputDirectory">The directory containing the input files.</param>
    /// <param name="outputDirectory">The directory to which the output files will be written.</param>
    /// <param name="inputMetadata">Metadata about the input variable.</param>
    /// <param name="targetMetadata">Metadata about the target variable.</param>
    /// <param name="inputTimeStep">The time step of the input variable.</param>
    /// <param name="outputTimeStep">The time step of the output variable.</param>
    /// <param name="aggregationMethod">The aggregation method to use.</param>
    /// <param name="gridFile">The grid file to use for remapping.</param>
    /// <param name="remapAlgorithm">The interpolation algorithm to use for remapping.</param>
    /// <param name="unpack">Whether to unpack the input files.</param>
    /// <param name="ncpus">The number of CPUs to use for processing.</param>
    /// <param name="dataset">The climate dataset being processed.</param>
    public PreprocessingOptions(
        string inputDirectory,
        string outputDirectory,
        VariableInfo inputMetadata,
        VariableInfo targetMetadata,
        TimeStep inputTimeStep,
        TimeStep outputTimeStep,
        AggregationMethod aggregationMethod,
        string? gridFile,
        InterpolationAlgorithm remapAlgorithm,
        bool unpack,
        int ncpus,
        IClimateDataset dataset)
    {
        InputDirectory = inputDirectory;
        OutputDirectory = outputDirectory;
        InputMetadata = inputMetadata;
        TargetMetadata = targetMetadata;
        InputTimeStep = inputTimeStep;
        OutputTimeStep = outputTimeStep;
        AggregationMethod = aggregationMethod;
        GridFile = gridFile;
        RemapAlgorithm = remapAlgorithm;
        Unpack = unpack;
        NCpus = ncpus;
        Dataset = dataset;
    }
}

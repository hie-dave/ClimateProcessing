using ClimateProcessing.Units;

namespace ClimateProcessing.Models.Options;

/// <summary>
/// Options describing how files should be merged together along their time
/// axes, as well as any other transformations that should be applied.
/// </summary>
public class MergetimeOptions : IMergetimeOptions
{
    /// <summary>
    /// The directory containing the input files.
    /// </summary>
    public string InputDirectory { get; private init; }

    /// <summary>
    /// The path to the output file.
    /// </summary>
    public string OutputFile { get; private init; }

    /// <summary>
    /// The metadata of the input variable.
    /// </summary>
    public VariableInfo InputMetadata { get; private init; }

    /// <summary>
    /// The metadata of the target variable.
    /// </summary>
    public VariableInfo TargetMetadata { get; private init; }

    /// <summary>
    /// The time step of the input variable.
    /// </summary>
    public TimeStep InputTimeStep { get; private init; }

    /// <summary>
    /// The target time step in the output file.
    /// </summary>
    public TimeStep OutputTimeStep { get; private init; }

    /// <summary>
    /// The aggregation method to use to temporally aggregate data.
    /// </summary>
    public AggregationMethod AggregationMethod { get; private init; }

    /// <summary>
    /// The optional grid file to use for remapping.
    /// </summary>
    public string? GridFile { get; private init; }

    /// <summary>
    /// The optional interpolation algorithm to use for remapping.
    /// </summary>
    public InterpolationAlgorithm RemapAlgorithm { get; private init; }

    /// <summary>
    /// Whether to unpack the input files.
    /// </summary>
    public bool Unpack { get; private init; }

    /// <summary>
    /// Whether to compress the output file.
    /// </summary>
    public bool Compress { get; private init; }

    /// <summary>
    /// The dataset to use for the mergetime operation.
    /// </summary>
    public IClimateDataset Dataset { get; private init; }

    /// <summary>
    /// The standard name of the target variable as required by the model.
    /// </summary>
    public string StandardName { get; private init; }

    /// <summary>
    /// Creates a new instance of <see cref="MergetimeOptions"/>.
    /// </summary>
    /// <param name="inputDirectory">The directory containing the input files.</param>
    /// <param name="outputFile">The path to the output file.</param>
    /// <param name="inputMetadata">The metadata of the input variable.</param>
    /// <param name="targetMetadata">The metadata of the target variable.</param>
    /// <param name="inputTimeStep">The time step of the input variable.</param>
    /// <param name="outputTimeStep">The target time step in the output file.</param>
    /// <param name="aggregationMethod">The aggregation method to use to temporally aggregate data.</param>
    /// <param name="gridFile">The optional grid file to use for remapping.</param>
    /// <param name="remapAlgorithm">The optional interpolation algorithm to use for remapping.</param>
    /// <param name="unpack">Whether to unpack the input files.</param>
    /// <param name="compress">Whether to compress the output file.</param>
    /// <param name="dataset">The dataset to use for the mergetime operation.</param>
    /// <param name="standardName">The standard name of the target variable as required by the model.</param>
    public MergetimeOptions(
        string inputDirectory,
        string outputFile,
        VariableInfo inputMetadata,
        VariableInfo targetMetadata,
        TimeStep inputTimeStep,
        TimeStep outputTimeStep,
        AggregationMethod aggregationMethod,
        string? gridFile,
        InterpolationAlgorithm remapAlgorithm,
        bool unpack,
        bool compress,
        IClimateDataset dataset,
        string standardName)
    {
        InputDirectory = inputDirectory;
        OutputFile = outputFile;
        InputMetadata = inputMetadata;
        TargetMetadata = targetMetadata;
        InputTimeStep = inputTimeStep;
        OutputTimeStep = outputTimeStep;
        AggregationMethod = aggregationMethod;
        GridFile = gridFile;
        RemapAlgorithm = remapAlgorithm;
        Unpack = unpack;
        Compress = compress;
        Dataset = dataset;
        StandardName = standardName;
    }
}

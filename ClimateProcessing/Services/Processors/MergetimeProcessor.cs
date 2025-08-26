using ClimateProcessing.Models;
using ClimateProcessing.Models.Options;

namespace ClimateProcessing.Services.Processors;

/// <summary>
/// A processor for merging multiple files into a single timeseries file. Note:
/// this processor does not perform any rechunking, and so is not suitable as a
/// standalone variable processor. The typical use-case here is mergetime-ing
/// files which are an intermediate requirement for another processor in the
/// pipeline.
/// </summary>
public class MergetimeProcessor : IVariableProcessor
{
    /// <summary>
    /// The mergetime script generator.
    /// </summary>
    private readonly IMergetimeScriptGenerator scriptGenerator;

    /// <inheritdoc />
    public ClimateVariable TargetVariable { get; private init; }

    /// <inheritdoc />
    public ClimateVariableFormat OutputFormat => ClimateVariableFormat.Timeseries(TargetVariable);

    /// <inheritdoc />
    public IEnumerable<ClimateVariableFormat> IntermediateOutputs => [];

    /// <inheritdoc />
    public IReadOnlySet<ClimateVariableFormat> Dependencies => new HashSet<ClimateVariableFormat>();

    /// <summary>
    /// Creates a new mergetime processor, using the standard mergetime script generator.
    /// </summary>
    /// <param name="targetVariable">The target variable.</param>
    public MergetimeProcessor(ClimateVariable targetVariable)
        : this(targetVariable, new CdoMergetimeScriptGenerator())
    {
    }

    /// <summary>
    /// Creates a new mergetime processor.
    /// </summary>
    /// <param name="targetVariable">The target variable.</param>
    /// <param name="scriptGenerator">The mergetime script generator.</param>
    public MergetimeProcessor(ClimateVariable targetVariable, IMergetimeScriptGenerator scriptGenerator)
    {
        TargetVariable = targetVariable;
        this.scriptGenerator = scriptGenerator;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Job>> CreateJobsAsync(IClimateDataset dataset, IJobCreationContext context)
    {
        VariableInfo inputMetadata = dataset.GetVariableInfo(TargetVariable);
        VariableInfo targetMetadata = context.VariableManager.GetOutputRequirements(TargetVariable);

        // File paths.
        string inDir = dataset.GetInputFilesDirectory(TargetVariable);

        // TODO: this will generate an output file name which uses the name of
        // the variable from the input dataset. This will be incorrect if the
        // user wants to rename the variable (the output file name will contain
        // the original variable name). It's not a huge problem but would be
        // better to fix.
        string outFile = context.PathManager.GetDatasetFileName(dataset, TargetVariable, PathType.Working, context.VariableManager);

        // Sanitise - e.g. /tmp/./x -> /tmp/x
        outFile = Path.GetFullPath(outFile);

        List<string> requiredFiles = [
            inDir,
            outFile
        ];
        if (!string.IsNullOrEmpty(context.Config.GridFile))
            requiredFiles.Add(context.Config.GridFile);
        IEnumerable<PBSStorageDirective> storageDirectives =
            PBSStorageHelper.GetStorageDirectives(requiredFiles);

        // Create script directory if it doesn't already exist.
        // This should be unnecessary at this point.
        string jobName = $"mergetime_{inputMetadata.Name}_{dataset.DatasetName}";
        using IFileWriter writer = context.FileWriterFactory.Create(jobName);
        await context.PBSLightweight.WritePBSHeader(writer, jobName, storageDirectives);

        MergetimeOptions opts = new MergetimeOptions(
            inDir,
            outFile,
            inputMetadata,
            targetMetadata,
            context.Config.InputTimeStep,
            context.Config.OutputTimeStep,
            context.VariableManager.GetAggregationMethod(TargetVariable),
            context.Config.GridFile,
            context.Remapper.GetInterpolationAlgorithm(inputMetadata, TargetVariable),
            false,
            false,
            dataset
        );

        await scriptGenerator.WriteMergetimeScriptAsync(writer, opts);

        Job job = new Job(
            jobName,
            writer.FilePath,
            ClimateVariableFormat.Timeseries(TargetVariable),
            outFile,
            [] // No dependencies
        );

        return [job];
    }
}

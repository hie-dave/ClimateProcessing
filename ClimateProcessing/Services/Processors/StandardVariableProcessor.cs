using ClimateProcessing.Models;
using ClimateProcessing.Models.Options;

namespace ClimateProcessing.Services.Processors;

/// <summary>
/// A processor for standard climate variables.
/// </summary>
/// <remarks>
/// This processor handles the processing of most climate variables:
/// - Merging multiple files into a single timeseries file (via cdo)
/// - Rechunking the timeseries file to optimise for LPJ-GUESS access patterns (via nco)
/// </remarks>
public class StandardVariableProcessor : IVariableProcessor
{
    /// <summary>
    /// The mergetime script generator.
    /// </summary>
    private readonly IMergetimeScriptGenerator mergetimeGenerator;

    /// <summary>
    /// The rechunking script generator.
    /// </summary>
    private readonly IRechunkScriptGenerator rechunkGenerator;

    /// <inheritdoc />
    public ClimateVariable TargetVariable { get; private init; }

    /// <inheritdoc />
    public ClimateVariableFormat OutputFormat => ClimateVariableFormat.Rechunked(TargetVariable);

    /// <inheritdoc />
    public IEnumerable<ClimateVariableFormat> IntermediateOutputs => [ClimateVariableFormat.Timeseries(TargetVariable)];

    /// <inheritdoc />
    public IReadOnlySet<ClimateVariableFormat> Dependencies => new HashSet<ClimateVariableFormat>();

    /// <summary>
    /// Creates a new standard variable processor, using the standard mergetime and rechunking scripts.
    /// </summary>
    /// <param name="targetVariable">The target variable.</param>
    public StandardVariableProcessor(ClimateVariable targetVariable)
        : this(targetVariable, new CdoMergetimeScriptGenerator(), new NcoRechunkScriptGenerator())
    {
    }

    /// <summary>
    /// Creates a new standard variable processor.
    /// </summary>
    /// <param name="targetVariable">The target variable.</param>
    /// <param name="mergetimeGenerator">The mergetime script generator.</param>
    /// <param name="rechunkingGenerator">The rechunking script generator.</param>
    /// <param name="pathManager">The path manager.</param>
    /// <param name="config">The processing configuration.</param>
    public StandardVariableProcessor(
        ClimateVariable targetVariable,
        IMergetimeScriptGenerator mergetimeGenerator,
        IRechunkScriptGenerator rechunkingGenerator)
    {
        TargetVariable = targetVariable;
        this.mergetimeGenerator = mergetimeGenerator;
        this.rechunkGenerator = rechunkingGenerator;
    }

    public async Task<IReadOnlyList<Job>> CreateJobsAsync(
        IClimateDataset dataset,
        IJobCreationContext context)
    {
        Job mergetime = await GenerateMergetimeJob(dataset, context);
        Job rechunk = await GenerateRechunkJob(dataset, context, mergetime);

        return [mergetime, rechunk];
    }

    /// <summary>
    /// Generate a mergetime script for the specified variable, and return the
    /// path to the generated script file.
    /// </summary>
    /// <param name="dataset">The dataset to process.</param>
    /// <param name="context">The job creation context.</param>
    /// <returns>The generated mergetime job.</returns>
    private async Task<Job> GenerateMergetimeJob(IClimateDataset dataset, IJobCreationContext context)
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
        string jobName = GetJobName("mergetime", inputMetadata, dataset);
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

        await mergetimeGenerator.WriteMergetimeScriptAsync(writer, opts);

        return new Job(
            jobName,
            writer.FilePath,
            ClimateVariableFormat.Timeseries(TargetVariable),
            outFile,
            [] // No dependencies
        );
    }

    /// <summary>
    /// Generate a rechunk job.
    /// </summary>
    /// <param name="dataset">The dataset to process.</param>
    /// <param name="context">The job creation context.</param>
    /// <param name="mergetimeJob">The mergetime job upon which this one depends.</param>
    /// <returns>The generated rechunk job.</returns>
    private async Task<Job> GenerateRechunkJob(
        IClimateDataset dataset,
        IJobCreationContext context,
        Job mergetimeJob)
    {
        VariableInfo varInfo = dataset.GetVariableInfo(TargetVariable);

        string jobName = GetJobName("rechunk", varInfo, dataset);
        string inFile = context.PathManager.GetDatasetFileName(dataset, TargetVariable, PathType.Working, context.VariableManager);
        string outFile = context.PathManager.GetDatasetFileName(dataset, TargetVariable, PathType.Output, context.VariableManager);

        // TODO: this will prevent early cleanup of resources in non-dave
        // versions, where we don't actually calculate VPD.
        bool cleanup = !VpdCalculator.IsVpdDependency(TargetVariable);

        using IFileWriter writer = context.FileWriterFactory.Create(jobName);
        IEnumerable<PBSStorageDirective> storageDirectives =
            PBSStorageHelper.GetStorageDirectives([inFile, outFile]);

        await context.PBSHeavyweight.WritePBSHeader(writer, jobName, storageDirectives);

        RechunkOptions options = new(
            inFile,
            outFile,
            context.Config.ChunkSizeSpatial,
            context.Config.ChunkSizeTime,
            context.Config.CompressionLevel,
            cleanup,
            context.PathManager
        );

        await rechunkGenerator.WriteRechunkScriptAsync(writer, options);

        return new Job(
            jobName,
            writer.FilePath,
            ClimateVariableFormat.Rechunked(TargetVariable),
            outFile,
            [mergetimeJob]
        );
    }

    /// <summary>
    /// Generate a job name.
    /// </summary>
    /// <param name="prefix">Job name prefix which provides context about the job.</param>
    /// <param name="info">Metadata for the variable in the dataset being processed.</param>
    /// <param name="dataset">The dataset being processed.</param>
    /// <returns>A job name.</returns>
    private static string GetJobName(string prefix, VariableInfo info, IClimateDataset dataset)
    {
        return $"{prefix}_{info.Name}_{dataset.DatasetName}";
    }
}

using ClimateProcessing.Models;
using ClimateProcessing.Models.Options;

namespace ClimateProcessing.Services.Processors;

/// <summary>
/// Processor which wraps an internal processing step and rechunks the output.
/// </summary>
public class RechunkingProcessorDecorator : IVariableProcessor
{
    /// <summary>
    /// The inner processor.
    /// </summary>
    private readonly IVariableProcessor innerProcessor;

    /// <summary>
    /// The rechunk script generator.
    /// </summary>
    private readonly IRechunkScriptGenerator rechunkGenerator;

    /// <summary>
    /// Creates a new rechunking processor decorator, using the standard rechunk script generator.
    /// </summary>
    /// <param name="innerProcessor">The inner processor.</param>
    public RechunkingProcessorDecorator(IVariableProcessor innerProcessor)
        : this(innerProcessor, new NcoRechunkScriptGenerator()) { }

    /// <summary>
    /// Creates a new rechunking processor decorator.
    /// </summary>
    /// <param name="innerProcessor">The inner processor.</param>
    /// <param name="rechunkGenerator">The rechunk script generator.</param>
    public RechunkingProcessorDecorator(
        IVariableProcessor innerProcessor,
        IRechunkScriptGenerator rechunkGenerator)
    {
        this.innerProcessor = innerProcessor;
        this.rechunkGenerator = rechunkGenerator;
    }

    /// <inheritdoc/>
    public ClimateVariable TargetVariable => innerProcessor.TargetVariable;

    /// <inheritdoc/>
    public IReadOnlySet<ClimateVariableFormat> Dependencies => innerProcessor.Dependencies;

    /// <inheritdoc/>
    ClimateVariable IVariableProcessor.TargetVariable => throw new NotImplementedException();

    /// <inheritdoc/>
    IReadOnlySet<ClimateVariableFormat> IVariableProcessor.Dependencies => throw new NotImplementedException();

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Job>> CreateJobsAsync(
        IClimateDataset dataset,
        IJobCreationContext context)
    {
        // Get the base jobs from the inner processor
        var baseJobs = await innerProcessor.CreateJobsAsync(dataset, context);

        // Find timeseries jobs that need rechunking
        var jobsNeedingRechunk = baseJobs
            .Where(j => j.Output.Stage == ProcessingStage.Timeseries)
            .ToList();

        // Create rechunking jobs
        List<Job> allJobs = new(baseJobs);
        foreach (var job in jobsNeedingRechunk)
        {
            var rechunkJob = await CreateRechunkJob(dataset, context, job);
            allJobs.Add(rechunkJob);
        }

        return allJobs;
    }

    private async Task<Job> CreateRechunkJob(
        IClimateDataset dataset,
        IJobCreationContext context,
        Job sourceJob)
    {
        // TODO: reuse code from StandardVariableProcessor
        // (e.g. GetJobName())
        string jobName = $"rechunk_{TargetVariable}_{dataset.DatasetName}";
        string inFile = sourceJob.OutputPath;
        string outFile = context.PathManager.GetDatasetFileName(dataset, TargetVariable, PathType.Output);
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
            // TODO: can we always remove inputs? Right now, yes. In general,
            // maybe not.
            true,
            context.PathManager
        );

        await rechunkGenerator.WriteRechunkScriptAsync(writer, options);

        return new Job(
            jobName,
            writer.FilePath,
            ClimateVariableFormat.Rechunked(TargetVariable),
            outFile,
            [sourceJob]
        );
    }
}
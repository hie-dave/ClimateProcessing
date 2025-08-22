using ClimateProcessing.Models;

namespace ClimateProcessing.Services.Processors;

/// <summary>
/// A variable processor which calculates the mean value at each time step of
/// the specified inputs.
/// </summary>
public class MeanProcessor : IVariableProcessor
{
    /// <summary>
    /// The name of the output file.
    /// </summary>
    private readonly string outputFileName;

    /// <summary>
    /// The variables to calculate the mean of.
    /// </summary>
    private readonly IEnumerable<ClimateVariable> dependencies;

    /// <inheritdoc/>
    public ClimateVariable TargetVariable { get; private init; }

    /// <inheritdoc/>
    public ClimateVariableFormat OutputFormat => ClimateVariableFormat.Timeseries(TargetVariable);

    /// <inheritdoc/>
    public IEnumerable<ClimateVariableFormat> IntermediateOutputs => [];

    /// <inheritdoc/>
    public IReadOnlySet<ClimateVariableFormat> Dependencies
        => new HashSet<ClimateVariableFormat>(dependencies.Select(ClimateVariableFormat.Timeseries));

    /// <summary>
    /// Creates a new instance of the <see cref="MeanProcessor"/> class.
    /// </summary>
    /// <param name="variable">The variable to calculate the mean of.</param>
    /// <param name="dependencies">The variables to calculate the mean of.</param>
    public MeanProcessor(
        string outputFileName,
        ClimateVariable variable,
        IEnumerable<ClimateVariable> dependencies)
    {
        this.outputFileName = outputFileName;
        TargetVariable = variable;
        this.dependencies = dependencies;
        if (dependencies.Count() < 2)
            throw new ArgumentException($"{GetType().Name} requires at least two dependencies.", nameof(dependencies));
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Job>> CreateJobsAsync(IClimateDataset dataset, IJobCreationContext context)
    {
        IEnumerable<string> inputFiles = dependencies.Select(x => context.PathManager.GetDatasetFileName(dataset, x, PathType.Working));
        string outputDirectory = context.PathManager.GetDatasetPath(dataset, PathType.Working);
        string outputFile = Path.Combine(outputDirectory, outputFileName);

        // Ensure we have no file conflicts.
        if (inputFiles.Any(i => i == outputFile))
            throw new ArgumentException($"Output file {outputFile} conflicts with an input file.");

        string[] requiredFiles = [.. inputFiles, outputFile];
        IEnumerable<PBSStorageDirective> storageDirectives = PBSStorageHelper.GetStorageDirectives(requiredFiles);

        string jobName = $"calc_mean_{TargetVariable}";
        using IFileWriter writer = context.FileWriterFactory.Create(jobName);

        await context.PBSLightweight.WriteHeaderAsync(writer, jobName, storageDirectives);

        await writer.WriteLineAsync("# File paths.");
        await writer.WriteLineAsync($"IN_FILES=\"{string.Join(" ", inputFiles)}\"");
        await writer.WriteLineAsync($"OUT_FILE=\"{outputFile}\"");

        string eqnFile = "${WORK_DIR}/mean_equations.txt";
        await writer.WriteLineAsync($"EQN_FILE=\"{eqnFile}\"");
        await writer.WriteLineAsync();

        await writer.WriteLineAsync("# Generate equation file.");
        await writer.WriteLineAsync("log \"Generating mean equation file...\"");

        // Create equation file with selected method.
        await writer.WriteLineAsync($"cat >\"${{EQN_FILE}}\" <<EOF");
        await WriteEquationsAsync(writer, context);
        await writer.WriteLineAsync("EOF");
        await writer.WriteLineAsync();

        // Calculate mean using the equation file.
        await writer.WriteLineAsync("# Calculate mean.");
        await writer.WriteLineAsync($"log \"Calculating mean...\"");
        await writer.WriteLineAsync($"cdo {CdoMergetimeScriptGenerator.GetCommonArgs()} exprf,\"${{EQN_FILE}}\" -merge ${{IN_FILES}} \"${{OUT_FILE}}\"");
        await writer.WriteLineAsync($"log \"Mean calculation completed successfully.\"");

        return [new Job(
            jobName,
            writer.FilePath,
            OutputFormat,
            outputFile,
            Dependencies.Select(context.DependencyResolver.GetJob))];
    }

    /// <summary>
    /// Write the equations for calculating the mean to a text writer.
    /// </summary>
    /// <param name="writer">The writer to write to.</param>
    private async Task WriteEquationsAsync(IFileWriter writer, IJobCreationContext context)
    {
        IEnumerable<string> inputVariables = dependencies.Select(d => context.VariableManager.GetOutputRequirements(d).Name);
        string outputVariableName = context.VariableManager.GetOutputRequirements(TargetVariable).Name;
        int denominator = dependencies.Count();
        await writer.WriteLineAsync($"{outputVariableName}=({string.Join(" + ", inputVariables)})/{denominator};");
    }
}

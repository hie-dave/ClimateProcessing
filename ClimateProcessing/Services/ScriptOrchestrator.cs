using ClimateProcessing.Models;
using ClimateProcessing.Configuration;
using ClimateProcessing.Models.Options;

namespace ClimateProcessing.Services;

/// <summary>
/// Default implementation of script generator that works with any climate dataset.
/// </summary>
public class ScriptOrchestrator : IScriptGenerator
{
    /// <summary>
    /// The file writer factory.
    /// </summary>
    protected readonly IFileWriterFactory fileWriterFactory;

    /// <summary>
    /// The processing configuration.
    /// </summary>
    protected readonly ProcessingConfig _config;

    /// <summary>
    /// The path manager service.
    /// </summary>
    protected readonly IPathManager pathManager;

    /// <summary>
    /// The climate variable manager service.
    /// </summary>
    protected readonly IClimateVariableManager variableManager;

    /// <summary>
    /// The PBS script generator service.
    /// </summary>
    protected readonly PBSWriter pbsHeavyweight;

    /// <summary>
    /// The PBS script generator service.
    /// </summary>
    protected readonly PBSWriter pbsLightweight;

    /// <summary>
    /// The remapping service.
    /// </summary>
    private readonly IRemappingService remappingService;

    /// <summary>
    /// Creates a new script generator with the default file writer factory.
    /// </summary>
    /// <param name="config">The processing configuration.</param>
    public ScriptOrchestrator(ProcessingConfig config) : this(
        config,
        new PathManager(config.OutputDirectory))
    {
        // TODO: replace with DI.
    }

    /// <summary>
    /// Creates a new script generator.
    /// </summary>
    /// <param name="config">The processing configuration.</param>
    /// <param name="pathManager">The path manager.</param>
    public ScriptOrchestrator(
        ProcessingConfig config,
        IPathManager pathManager) : this(config,
                                         pathManager,
                                         new FileWriterFactory(pathManager),
                                         new RemappingService())
    {
        // TODO: replace with DI.
    }

    /// <summary>
    /// Creates a new script generator.
    /// </summary>
    /// <param name="config">The processing configuration.</param>
    /// <param name="pathManager">The path manager.</param>
    /// <param name="fileWriterFactory">The file writer factory.</param>
    /// <param name="remappingService">The remapping service.</param>
    public ScriptOrchestrator(
        ProcessingConfig config,
        IPathManager pathManager,
        IFileWriterFactory fileWriterFactory,
        IRemappingService remappingService)
    {
        _config = config;
        this.pathManager = pathManager;
        this.fileWriterFactory = fileWriterFactory;
        this.remappingService = remappingService;

        PBSWalltime walltime = PBSWalltime.Parse(config.Walltime);
        PBSConfig pbsConfig = new(
            config.Queue,
            config.Ncpus,
            config.Memory,
            config.JobFS,
            config.Project,
            walltime,
            config.EmailNotifications,
            config.Email
        );
        pbsHeavyweight = new PBSWriter(pbsConfig, pathManager);

        PBSConfig lightweightConfig = PBSConfig.LightWeight(
            config.JobFS,
            config.Project,
            config.EmailNotifications,
            config.Email,
            walltime
        );
        pbsLightweight = new PBSWriter(lightweightConfig, pathManager);
        variableManager = new ClimateVariableManager(config.Version);
    }

    /// <summary>
    /// Generate processing scripts, and return the path to the top-level script.
    /// </summary>
    /// <param name="dataset">The dataset.</param>
    /// <returns>The path to the top-level script.</returns>
    public async Task<string> GenerateScriptsAsync(IClimateDataset dataset)
    {
        pathManager.CreateDirectoryTree(dataset);

        DependencyResolver dependencyResolver = new DependencyResolver();

        JobCreationContext context = new JobCreationContext(
            _config,
            pathManager,
            fileWriterFactory,
            variableManager,
            pbsLightweight,
            pbsHeavyweight,
            remappingService,
            dependencyResolver
        );

        // Process each variable.
        // TODO: jobs must be created in order, so that dependencies are satisfied.
        IEnumerable<IVariableProcessor> variables = dataset.GetProcessors(context);
        foreach (IVariableProcessor processor in variables)
        {
            IEnumerable<Job> jobs = await processor.CreateJobsAsync(dataset, context);
            dependencyResolver.AddJobs(jobs);
        }

        string cleanupScript = await GenerateCleanupScript(dataset);

        // Add job submission logic.
        bool requiresVpd = _config.Version == ModelVersion.Dave;
        bool vpdEmpty = true;
        bool allDepsEmpty = true;

        // Build job submission script.
        string submitJobName = $"submit_{dataset.DatasetName}";
        using IFileWriter submitScript = fileWriterFactory.Create(submitJobName);

        // Add PBS header.
        await submitScript.WriteLineAsync("#!/usr/bin/env bash");
        await submitScript.WriteLineAsync($"# Job submission script for: {dataset.DatasetName}");
        await submitScript.WriteLineAsync();
        await WriteAutoGenerateHeader(submitScript);
        await submitScript.WriteLineAsync("# Exit immediately if any command fails.");
        await submitScript.WriteLineAsync("set -euo pipefail");
        await submitScript.WriteLineAsync();

        // Ensure output directory exists.
        await submitScript.WriteLineAsync($"mkdir -p \"{_config.OutputDirectory}\"");
        await submitScript.WriteLineAsync();

        foreach (IVariableProcessor variable in variables)
        {
            // Submit mergetime script.
            await submitScript.WriteLineAsync($"JOB_ID=\"$(qsub \"{mergetimeScripts[variable]}\")\"");

            // Append this job to the list of VPD dependencies if necessary.
            if (requiresVpd && VpdCalculator.IsVpdDependency(variable))
            {
                if (vpdEmpty)
                    await submitScript.WriteLineAsync("VPD_DEPS=\"${JOB_ID}\"");
                else
                    await submitScript.WriteLineAsync($"VPD_DEPS=\"${{VPD_DEPS}}:${{JOB_ID}}\"");
                vpdEmpty = false;
            }

            bool variableRequired = true;
            if (variable == ClimateVariable.SpecificHumidity && _config.Version == ModelVersion.Dave)
                variableRequired = false;

            if (variableRequired)
            {
                // DAVE version doesn't require specific humidity. We need to do
                // the mergetime step, because that's used as an input for the
                // VPD computation, but the quantity itself is not used by the
                // model and we therefore don't need to rechunk it.

                // Submit rechunk script.
                await submitScript.WriteLineAsync($"JOB_ID=\"$(qsub -W depend=afterok:\"${{JOB_ID}}\" \"{rechunkScripts[variable]}\")\"");

                // Append the ID of the rechunk job to the "all jobs" list.
                if (allDepsEmpty)
                {
                    await submitScript.WriteLineAsync("ALL_JOBS=\"${JOB_ID}\"");
                    allDepsEmpty = false;
                }
                else
                    await submitScript.WriteLineAsync($"ALL_JOBS=\"${{ALL_JOBS}}:${{JOB_ID}}\"");
            }
            await submitScript.WriteLineAsync();
        }

        // Submit VPD scripts.
        if (requiresVpd)
        {
            await submitScript.WriteLineAsync($"JOB_ID=\"$(qsub -W depend=afterok:\"${{VPD_DEPS}}\" \"{vpdScript}\")\"");
            await submitScript.WriteLineAsync($"JOB_ID=\"$(qsub -W depend=afterok:\"${{JOB_ID}}\" \"{vpdRechunkScript}\")\"");
            await submitScript.WriteLineAsync($"ALL_JOBS=\"${{ALL_JOBS}}:${{JOB_ID}}\"");
            await submitScript.WriteLineAsync();
        }

        // Submit cleanup script.
        await submitScript.WriteLineAsync($"JOB_ID=\"$(qsub -W depend=afterok:\"${{ALL_JOBS}}\" \"{cleanupScript}\")\"");
        await submitScript.WriteLineAsync();

        await submitScript.WriteLineAsync($"echo \"Job submission complete for dataset {dataset.DatasetName}. Job ID: ${{JOB_ID}}\"");
        await submitScript.WriteLineAsync();

        return submitScript.FilePath;
    }

    /// <summary>
    /// Generate a wrapper script that executes the given scripts.
    /// </summary>
    /// <param name="scripts">The script files to execute.</param>
    public static async Task<string> GenerateWrapperScript(string outputDirectory, IEnumerable<string> scripts)
    {
        PathManager pathManager = new(outputDirectory);

        string jobName = "wrapper";
        string scriptPath = pathManager.GetBasePath(PathType.Script);
        string scriptFile = Path.Combine(scriptPath, jobName);

        using IFileWriter writer = new ScriptWriter(scriptFile);

        await writer.WriteLineAsync("#!/usr/bin/env bash");
        await writer.WriteLineAsync("# Master-level script which executes all job submission scripts to submit all PBS jobs.");
        await writer.WriteLineAsync();

        await WriteAutoGenerateHeader(writer);

        await writer.WriteLineAsync("set -euo pipefail");
        await writer.WriteLineAsync();

        // Execute all scripts (making assumptions about file permissions).
        foreach (string script in scripts)
            await writer.WriteLineAsync(script);

        return scriptFile;
    }

    /// <summary>
    /// Generate a cleanup script for this job.
    /// </summary>
    /// <param name="dataset">The dataset to process.</param>
    /// <returns>The path to the script file.</returns>
    private async Task<string> GenerateCleanupScript(IClimateDataset dataset)
    {
        string jobName = $"cleanup_{dataset.DatasetName}";
        using IFileWriter writer = fileWriterFactory.Create(jobName);
        string workDir = pathManager.GetDatasetPath(dataset, PathType.Working);
        IEnumerable<PBSStorageDirective> storageDirectives =
            PBSStorageHelper.GetStorageDirectives([workDir]);

        await pbsLightweight.WritePBSHeader(writer, jobName, storageDirectives);
        await writer.WriteLineAsync("# File paths.");
        await writer.WriteLineAsync($"IN_DIR=\"{workDir}\"");
        await writer.WriteLineAsync("rm -rf \"${IN_DIR}\"");

        return writer.FilePath;
    }

    /// <summary>
    /// Write a comment to a script which indicates that it was automatically
    /// generated.
    /// </summary>
    /// <param name="writer">The text writer to which the comment will be written.</param>
    private static async Task WriteAutoGenerateHeader(IFileWriter writer)
    {
        await writer.WriteLineAsync("# This script was automatically generated. Do not modify.");
        await writer.WriteLineAsync();
    }
}

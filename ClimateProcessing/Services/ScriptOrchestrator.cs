using ClimateProcessing.Models;
using ClimateProcessing.Configuration;
using ClimateProcessing.Models.Options;
using System.Text;

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
    /// The variable processor sorter service.
    /// </summary>
    private readonly IVariableProcessorSorter variableProcessorSorter;

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
        IRemappingService remappingService) : this(
            config,
            pathManager,
            fileWriterFactory,
            remappingService,
            new VariableProcessorSorter())
    {
    }

    /// <summary>
    /// Creates a new script generator.
    /// </summary>
    /// <param name="config">The processing configuration.</param>
    /// <param name="pathManager">The path manager.</param>
    /// <param name="fileWriterFactory">The file writer factory.</param>
    /// <param name="remappingService">The remapping service.</param>
    /// <param name="variableProcessorSorter">The variable processor sorter.</param>
    public ScriptOrchestrator(
        ProcessingConfig config,
        IPathManager pathManager,
        IFileWriterFactory fileWriterFactory,
        IRemappingService remappingService,
        IVariableProcessorSorter variableProcessorSorter)
    {
        _config = config;
        this.pathManager = pathManager;
        this.fileWriterFactory = fileWriterFactory;
        this.remappingService = remappingService;
        this.variableProcessorSorter = variableProcessorSorter;

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
        // Sort processors to ensure dependencies are satisfied
        IEnumerable<IVariableProcessor> variables = variableProcessorSorter.SortByDependencies(
            dataset.GetProcessors(context));

        // Create all jobs for all processors
        foreach (IVariableProcessor processor in variables)
        {
            IEnumerable<Job> jobs = await processor.CreateJobsAsync(dataset, context);
            dependencyResolver.AddJobs(jobs);
        }

        string cleanupScript = await GenerateCleanupScript(dataset);

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
        await submitScript.WriteLineAsync($"mkdir -p \"{_config.OutputDirectory}\"");
        await submitScript.WriteLineAsync();
        await submitScript.WriteLineAsync("# Dictionary to track job IDs");
        await submitScript.WriteLineAsync("declare -A JOB_IDS");
        await submitScript.WriteLineAsync();
        await submitScript.WriteLineAsync("# List to track all job IDs for cleanup dependency");
        await submitScript.WriteLineAsync("ALL_JOBS=\"\"");
        await submitScript.WriteLineAsync();
        await submitScript.WriteLineAsync("# Function to submit a job");
        await submitScript.WriteLineAsync("submit_job() {");
        await submitScript.WriteLineAsync("    local job_name=\"${1}\"");
        await submitScript.WriteLineAsync("    local script_path=\"${2}\"");
        await submitScript.WriteLineAsync("    shift 2");
        await submitScript.WriteLineAsync("    local dependency_jobs=(\"$@\")");
        await submitScript.WriteLineAsync();
        await submitScript.WriteLineAsync("    # Build dependency string if we have dependencies");
        await submitScript.WriteLineAsync("    local deps=\"\"");
        await submitScript.WriteLineAsync("    if [ ${#dependency_jobs[@]} -gt 0 ]; then");
        await submitScript.WriteLineAsync("        for dep_job in \"${dependency_jobs[@]}\"; do");
        await submitScript.WriteLineAsync("            if [ -z \"${dep_job}\" ]; then");
        await submitScript.WriteLineAsync("                continue");
        await submitScript.WriteLineAsync("            fi");
        await submitScript.WriteLineAsync("            if [ -z \"${deps}\" ]; then");
        await submitScript.WriteLineAsync("                deps=\"${JOB_IDS[${dep_job}]}\"");
        await submitScript.WriteLineAsync("            else");
        await submitScript.WriteLineAsync("                deps=\"${deps}:${JOB_IDS[${dep_job}]}\"");
        await submitScript.WriteLineAsync("            fi");
        await submitScript.WriteLineAsync("        done");
        await submitScript.WriteLineAsync("    fi");
        await submitScript.WriteLineAsync();
        await submitScript.WriteLineAsync("    if [ -z \"${deps}\" ]; then");
        await submitScript.WriteLineAsync("        # No dependencies");
        await submitScript.WriteLineAsync("        JOB_ID=\"$(qsub \"${script_path}\")\"");
        await submitScript.WriteLineAsync("    else");
        await submitScript.WriteLineAsync("        # With dependencies");
        await submitScript.WriteLineAsync("        JOB_ID=\"$(qsub -W depend=afterok:\"${deps}\" \"${script_path}\")\"");
        await submitScript.WriteLineAsync("    fi");
        await submitScript.WriteLineAsync();
        await submitScript.WriteLineAsync("    # Store the job ID");
        await submitScript.WriteLineAsync("    JOB_IDS[\"${job_name}\"]=\"${JOB_ID}\"");
        await submitScript.WriteLineAsync();
        await submitScript.WriteLineAsync("    # Add to all jobs list");
        await submitScript.WriteLineAsync("    if [ -z \"${ALL_JOBS}\" ]; then");
        await submitScript.WriteLineAsync("        ALL_JOBS=\"${JOB_ID}\"");
        await submitScript.WriteLineAsync("    else");
        await submitScript.WriteLineAsync("        ALL_JOBS=\"${ALL_JOBS}:${JOB_ID}\"");
        await submitScript.WriteLineAsync("    fi");
        await submitScript.WriteLineAsync();
        await submitScript.WriteLineAsync("    echo \"Submitted job ${job_name}: ${JOB_ID}\"");
        await submitScript.WriteLineAsync("}");
        await submitScript.WriteLineAsync();
        
        // Get all jobs from the dependency resolver
        IEnumerable<Job> allJobs = dependencyResolver.GetJobs();

        // Submit jobs in topological order (no job is submitted before its dependencies)
        // We can use a simple approach since our jobs are already created with proper dependencies
        HashSet<Job> submittedJobs = new HashSet<Job>();
        
        // Submit all jobs
        foreach (Job job in allJobs)
        {
            await SubmitJobRecursively(job, submittedJobs, submitScript);
        }
        
        // Submit cleanup script
        await submitScript.WriteLineAsync("# Submit cleanup script");
        await submitScript.WriteLineAsync($"JOB_ID=\"$(qsub -W depend=afterok:\"${{ALL_JOBS}}\" \"{cleanupScript}\")\"");
        await submitScript.WriteLineAsync();

        await submitScript.WriteLineAsync($"echo \"Job submission complete for dataset {dataset.DatasetName}. Final job ID: ${{JOB_ID}}\"");

        return submitScript.FilePath;
    }

    /// <summary>
    /// Recursively submits a job and its dependencies to the submission script.
    /// </summary>
    /// <param name="job">The job to submit.</param>
    /// <param name="submittedJobs">Set of jobs that have already been submitted.</param>
    /// <param name="submitScript">The script writer to write submission commands to.</param>
    private async Task SubmitJobRecursively(Job job, HashSet<Job> submittedJobs, IFileWriter submitScript)
    {
        // If we've already submitted this job, we're done
        if (submittedJobs.Contains(job))
        {
            return;
        }

        // First, submit all dependencies
        foreach (Job dependency in job.Dependencies)
        {
            await SubmitJobRecursively(dependency, submittedJobs, submitScript);
        }

        // Now submit this job
        await submitScript.WriteLineAsync($"# Submit job: {job.Name}");
        
        if (job.Dependencies.Count == 0)
        {
            // No dependencies
            await submitScript.WriteLineAsync($"submit_job \"{job.Name}\" \"{job.ScriptPath}\"");
        }
        else
        {
            // Build command with dependency job names as additional arguments
            StringBuilder command = new StringBuilder();
            command.Append($"submit_job \"{job.Name}\" \"{job.ScriptPath}\"")
                  .Append(' ');
            
            // Add each dependency job name as an argument
            foreach (Job dependency in job.Dependencies)
            {
                command.Append($"\"{dependency.Name}\" ");
            }
            
            await submitScript.WriteLineAsync(command.ToString().TrimEnd());
        }
        
        await submitScript.WriteLineAsync();
        
        // Mark as submitted
        submittedJobs.Add(job);
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

        await pbsLightweight.WriteHeaderAsync(writer, jobName, storageDirectives);
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

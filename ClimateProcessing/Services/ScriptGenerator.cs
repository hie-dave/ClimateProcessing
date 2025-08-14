using ClimateProcessing.Models;
using ClimateProcessing.Units;
using System.Runtime.CompilerServices;
using ClimateProcessing.Extensions;
using System.Text.RegularExpressions;
using ClimateProcessing.Configuration;

[assembly: InternalsVisibleTo("ClimateProcessing.Tests")]

namespace ClimateProcessing.Services;

/// <summary>
/// Default implementation of script generator that works with any climate dataset.
/// </summary>
public class ScriptGenerator : IScriptGenerator<IClimateDataset>
{
    /// <summary>
    /// CDO's conservative remapping operator.
    /// </summary>
    private const string remapConservative = "-remapcon";

    /// <summary>
    /// CDO's bilinear remapping operator.
    /// </summary>
    private const string remapBilinear = "remapbil";

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
    /// The VPD calculator service.
    /// </summary>
    private readonly VpdCalculator vpdCalculator;

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
    /// Name of the variable containing the directory holding all input files
    /// used as operands for the remap command.
    /// </summary>
    protected const string inDirVariable = "IN_DIR";

    /// <summary>
    /// Name of the variable containing the directory holding all remapped input
    /// files used as operands for the mergetime command.
    /// </summary>
    protected const string remapDirVariable = "REMAP_DIR";

    /// <summary>
    /// Creates a new script generator with the default file writer factory.
    /// </summary>
    /// <param name="config">The processing configuration.</param>
    public ScriptGenerator(ProcessingConfig config) : this(
        config,
        new PathManager(config.OutputDirectory))
    {
        // TODO: replace with DI.
    }

    public ScriptGenerator(
        ProcessingConfig config,
        IPathManager pathManager) : this(config,
                                         pathManager,
                                         new FileWriterFactory(pathManager))
    {
        // TODO: replace with DI.
    }

    /// <summary>
    /// Creates a new script generator.
    /// </summary>
    /// <param name="config">The processing configuration.</param>
    /// <param name="pathManager">The path manager.</param>
    /// <param name="fileWriterFactory">The file writer factory.</param>
    public ScriptGenerator(
        ProcessingConfig config,
        IPathManager pathManager,
        IFileWriterFactory fileWriterFactory)
    {
        _config = config;
        this.pathManager = pathManager;
        this.fileWriterFactory = fileWriterFactory;

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

        vpdCalculator = new VpdCalculator(
            config.VPDMethod,
            pathManager,
            fileWriterFactory);

        variableManager = new ClimateVariableManager(config.Version);
    }

    /// <summary>
    /// Generates the operator to rename a variable.
    /// </summary>
    /// <param name="inName">The name of the input variable.</param>
    /// <param name="outName">The name of the output variable.</param>
    /// <returns>The CDO operator to use for renaming.</returns>
    internal string GenerateRenameOperator(string inName, string outName)
    {
        if (inName == outName)
            return string.Empty;
        return $"-chname,'{inName}','{outName}'";
    }

    /// <summary>
    /// Generates the operators needed to convert the units of a variable.
    /// </summary>
    /// <param name="outputVar">The name of the output variable.</param>
    /// <param name="inputUnits">The units of the input variable.</param>
    /// <param name="targetUnits">The units of the output variable.</param>
    /// <param name="timeStep">The time step of the variable.</param>
    /// <returns>The CDO operators needed to convert the units.</returns>
    internal IEnumerable<string> GenerateUnitConversionOperators(
        string outputVar,
        string inputUnits,
        string targetUnits,
        TimeStep timeStep)
    {
        var result = UnitConverter.AnalyseConversion(inputUnits, targetUnits);

        List<string> operators = [];

        if (result.RequiresConversion)
        {
            string expression = UnitConverter.GenerateConversionExpression(
                inputUnits,
                targetUnits,
                timeStep);
            operators.Add(expression);
        }

        if (result.RequiresRenaming)
            operators.Add($"-setattribute,'{outputVar}@units={targetUnits}'");

        return operators;
    }

    /// <summary>
    /// Generate the operator to temporally aggregate the data.
    /// </summary>
    /// <param name="variable">The variable to aggregate.</param>
    /// <returns>The CDO operator to use for temporal aggregation.</returns>
    internal string GenerateTimeAggregationOperator(
        ClimateVariable variable)
    {
        // Only aggregate if input and output timesteps differ
        if (_config.InputTimeStep == _config.OutputTimeStep)
            return string.Empty;

        // Calculate the number of timesteps to aggregate
        int stepsToAggregate = _config.OutputTimeStep.Hours / _config.InputTimeStep.Hours;

        var aggregationMethod = variableManager.GetAggregationMethod(variable);
        var @operator = aggregationMethod.ToCdoOperator(_config.OutputTimeStep);

        return $"-{@operator},{stepsToAggregate}";
    }

    /// <summary>
    /// Standard arguments used for all CDO invocations.
    /// TODO: make verbosity configurable?
    /// </summary>
    private string GetCDOArgs()
    {
        return $"-L -O -v -z zip1";
    }

    /// <summary>
    /// Generate a script for rechunking VPD data.
    /// </summary>
    /// <param name="dataset">The dataset.</param>
    /// <returns>The script path.</returns>
    public async Task<string> GenerateVPDRechunkScript(IClimateDataset dataset, string inFile)
    {
        string jobName = $"rechunk_vpd_{dataset.DatasetName}";
        string temperatureFile = pathManager.GetDatasetFileName(dataset, ClimateVariable.Temperature, PathType.Output);
        string outFile = VpdCalculator.GetVpdFilePath(dataset, temperatureFile);
        return await GenerateRechunkScript(jobName, inFile, outFile, true);
    }

    /// <summary>
    /// Generate processing scripts, and return the path to the top-level script.
    /// </summary>
    /// <param name="dataset">The dataset.</param>
    /// <returns>The path to the top-level script.</returns>
    public async Task<string> GenerateScriptsAsync(IClimateDataset dataset)
    {
        pathManager.CreateDirectoryTree(dataset);

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

        // Process each variable.
        Dictionary<ClimateVariable, string> mergetimeScripts = new();
        Dictionary<ClimateVariable, string> rechunkScripts = new();
        IEnumerable<ClimateVariable> variables = variableManager.GetRequiredVariables();
        foreach (ClimateVariable variable in variables)
        {
            string mergetime = await GenerateVariableMergeScript(dataset, variable);
            string rechunk = await GenerateVariableRechunkScript(dataset, variable);

            mergetimeScripts[variable] = mergetime;
            rechunkScripts[variable] = rechunk;
        }

        string vpdScript = await vpdCalculator.GenerateVPDScript(dataset, pbsLightweight, GetCDOArgs());
        string vpdFile = vpdCalculator.GetUnoptimisedVpdOutputFilePath(dataset);
        string vpdRechunkScript = await GenerateVPDRechunkScript(dataset, vpdFile);
        string cleanupScript = await GenerateCleanupScript(dataset);

        // Add job submission logic.
        bool requiresVpd = _config.Version == ModelVersion.Dave;
        bool vpdEmpty = true;
        bool allDepsEmpty = true;

        foreach (ClimateVariable variable in variables)
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
    /// Write the pre-merge commands to the specified writer.
    /// </summary>
    /// <param name="writer">The text writer.</param>
    /// <param name="dataset">The climate dataset being processed.</param>
    /// <param name="variable">The variable of the dataset being processed.</param>
    protected virtual Task WritePreMerge(IFileWriter writer, IClimateDataset dataset, ClimateVariable variable)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Generate a job name.
    /// </summary>
    /// <param name="prefix">Job name prefix which provides context about the job.</param>
    /// <param name="info">Metadata for the variable in the dataset being processed.</param>
    /// <param name="dataset">The dataset being processed.</param>
    /// <returns>A job name.</returns>
    private string GetJobName(string prefix, VariableInfo info, IClimateDataset dataset)
    {
        return $"{prefix}_{info.Name}_{dataset.DatasetName}";
    }

    /// <summary>
    /// Sanitise a string to be stored in a bash variable.
    /// </summary>
    /// <param name="input">The string.</param>
    /// <returns>The sanitised string.</returns>
    internal static string SanitiseString(string input)
    {
        return input.Replace("$", "\\$");
    }

    /// <summary>
    /// Generate a mergetime script for the specified variable, and return the
    /// path to the generated script file.
    /// </summary>
    /// <param name="dataset">The dataset to process.</param>
    /// <param name="variable">The variable to process.</param>
    /// <param name="outFile">The path to the output file that should be generated by the script.</param>
    /// <returns>The path to the generated script file.</returns>
    internal async Task<string> GenerateVariableMergeScript(IClimateDataset dataset, ClimateVariable variable)
    {
        VariableInfo inputMetadata = dataset.GetVariableInfo(variable);
        VariableInfo targetMetadata = variableManager.GetOutputRequirements(variable);

        // File paths.
        string inDir = dataset.GetInputFilesDirectory(variable);

        string outFile = pathManager.GetDatasetFileName(dataset, variable, PathType.Working);

        // Sanitise - e.g. /tmp/./x -> /tmp/x
        outFile = Path.GetFullPath(outFile);

        List<string> requiredFiles = [
            inDir,
            outFile
        ];
        if (!string.IsNullOrEmpty(_config.GridFile))
            requiredFiles.Add(_config.GridFile);
        IEnumerable<PBSStorageDirective> storageDirectives =
            PBSStorageHelper.GetStorageDirectives(requiredFiles);

        // Create script directory if it doesn't already exist.
        // This should be unnecessary at this point.
        string jobName = GetJobName("mergetime", inputMetadata, dataset);
        using IFileWriter writer = fileWriterFactory.Create(jobName);
        await pbsLightweight.WritePBSHeader(writer, jobName, storageDirectives);

        await writer.WriteLineAsync("# File paths.");
        await writer.WriteLineAsync($"{inDirVariable}=\"{SanitiseString(inDir)}\"");
        if (!string.IsNullOrEmpty(_config.GridFile))
            await writer.WriteLineAsync($"{remapDirVariable}=\"${{WORK_DIR}}/remap\"");
        await writer.WriteLineAsync($"OUT_FILE=\"{SanitiseString(outFile)}\"");
        if (!string.IsNullOrEmpty(_config.GridFile))
            await writer.WriteLineAsync($"GRID_FILE=\"{SanitiseString(_config.GridFile)}\"");
        await writer.WriteLineAsync();

        if (!string.IsNullOrEmpty(_config.GridFile))
        {
            await writer.WriteLineAsync($"mkdir -p \"${{{remapDirVariable}}}\"");
            await writer.WriteLineAsync();
        }

        await WritePreMerge(writer, dataset, variable);

        string rename = GenerateRenameOperator(inputMetadata.Name, targetMetadata.Name);
        string conversion = string.Join(" ", GenerateUnitConversionOperators(targetMetadata.Name, inputMetadata.Units, targetMetadata.Units, _config.InputTimeStep));
        string aggregation = GenerateTimeAggregationOperator(variable);
        string unpack = "-unpack";
        string remapOperator = GetRemapOperator(GetInterpolationAlgorithm(inputMetadata, variable));
        string remap = string.IsNullOrEmpty(_config.GridFile) ? string.Empty : $"-{remapOperator},\"${{GRID_FILE}}\"";
        string operators = $"{aggregation} {conversion} {rename} {unpack} {remap}";
        operators = Regex.Replace(operators, " +", " ");

        // The above operators all take a single file as input; therefore we
        // must perform them as a separate step to the mergetime.
        if (!string.IsNullOrWhiteSpace(operators))
        {
            // Write description of processing steps.
            await writer.WriteLineAsync("# Perform corrective operations on input files:");
            if (!string.IsNullOrEmpty(remap))
                await writer.WriteLineAsync("# - Remap input files to target grid.");
            if (!string.IsNullOrEmpty(unpack))
                await writer.WriteLineAsync("# - Unpack data.");
            if (!string.IsNullOrEmpty(rename))
                await writer.WriteLineAsync($"# - Rename variable from {inputMetadata.Name} to {targetMetadata.Name}.");
            if (!string.IsNullOrEmpty(conversion))
                await writer.WriteLineAsync($"# - Convert units from {inputMetadata.Units} to {targetMetadata.Units}.");
            if (!string.IsNullOrEmpty(aggregation))
                await writer.WriteLineAsync($"# - Aggregate data from {_config.InputTimeStep} to {_config.OutputTimeStep}.");

            await writer.WriteLineAsync($"for FILE in \"${{{inDirVariable}}}\"/*.nc");
            await writer.WriteLineAsync($"do");
            await writer.WriteLineAsync($"    cdo {GetCDOArgs()} {operators} \"${{FILE}}\" \"${{{remapDirVariable}}}/$(basename \"${{FILE}}\")\"");
            await writer.WriteLineAsync("done");
            await writer.WriteLineAsync($"{inDirVariable}=\"${{{remapDirVariable}}}\"");
            await writer.WriteLineAsync();
        }

        // Merge files and perform all operations in a single step.
        await writer.WriteLineAsync("log \"Merging files...\"");
        await writer.WriteLineAsync($"cdo {GetCDOArgs()} mergetime \"${{{inDirVariable}}}\"/*.nc \"${{OUT_FILE}}\"");
        await writer.WriteLineAsync("log \"All files merged successfully.\"");
        await writer.WriteLineAsync();

        // Remapped files are in jobfs, and will be automatically deleted upon
        // job completion (or failure).

        return writer.FilePath;
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
    /// Generate a rechunking script.
    /// </summary>
    /// <param name="dataset">The dataset.</param>
    /// <param name="variable">The variable.</param>
    /// <param name="inputFile">The path to the file emitted by the mergetime script.</param>
    /// <remarks>
    /// This is separate to the mergetime script, because it's more
    /// resource-intensive. We can reduce costs by having the mergetime script
    /// run on a low-memory node, but the rechunking script needs a high-memory
    /// node.</remarks>
    /// <returns>Path to the generated script file.</returns>
    internal async Task<string> GenerateVariableRechunkScript(IClimateDataset dataset, ClimateVariable variable)
    {
        VariableInfo varInfo = dataset.GetVariableInfo(variable);

        string jobName = GetJobName("rechunk", varInfo, dataset);
        string inFile = pathManager.GetDatasetFileName(dataset, variable, PathType.Working);
        string outFile = pathManager.GetDatasetFileName(dataset, variable, PathType.Output);
        bool cleanup = !VpdCalculator.IsVpdDependency(variable);

        return await GenerateRechunkScript(jobName, inFile, outFile, cleanup);
    }

    private async Task<string> GenerateRechunkScript(string jobName, string inFile, string outFile, bool cleanup)
    {
        using IFileWriter writer = fileWriterFactory.Create(jobName);
        IEnumerable<PBSStorageDirective> storageDirectives =
            PBSStorageHelper.GetStorageDirectives([inFile, outFile]);

        await pbsHeavyweight.WritePBSHeader(writer, jobName, storageDirectives);

        // File paths.
        // The output of the mergetime script is the input file for this script.
        await writer.WriteLineAsync($"IN_FILE=\"{inFile}\"");
        await writer.WriteLineAsync($"OUT_FILE=\"{outFile}\"");
        // Reorder dimensions, improve chunking, and enable compression.

        // Note: we could use lon,lat,time but ncview works better if the
        // x-dimension precedes the y-dimension.
        string ordering = "-a lat,lon,time";
        string chunking = $"--cnk_dmn lat,{_config.ChunkSizeSpatial} --cnk_dmn lon,{_config.ChunkSizeSpatial} --cnk_dmn time,{_config.ChunkSizeTime}";
        string compression = _config.CompressOutput ? $"-L{_config.CompressionLevel}" : "";

        await writer.WriteLineAsync("log \"Rechunking files...\"");
        await writer.WriteLineAsync($"ncpdq -O {ordering} {chunking} {compression} \"${{IN_FILE}}\" \"${{OUT_FILE}}\"");
        await writer.WriteLineAsync("log \"All files rechunked successfully.\"");
        await writer.WriteLineAsync();

        // Calculate checksum.
        // Note: we change directory and use a relative file path, to ensure
        // that the checksum file remains portable.

        string outputPath = pathManager.GetBasePath(PathType.Output);
        string checksumFile = pathManager.GetChecksumFilePath();
        string relativePath = Path.GetRelativePath(outputPath, outFile);

        await writer.WriteLineAsync("# Calculate checksum.");
        await writer.WriteLineAsync($"log \"Calculating checksum...\"");
        await writer.WriteLineAsync($"cd \"{outputPath}\"");
        await writer.WriteLineAsync($"REL_PATH=\"{relativePath}\"");
        await writer.WriteLineAsync($"sha512sum \"${{REL_PATH}}\" >>\"{checksumFile}\"");
        await writer.WriteLineAsync("log \"Checksum calculation completed successfully.\"");
        await writer.WriteLineAsync();

        // We can now delete the temporary input file, but only if it's not also
        // required for the VPD estimation, which may not have occurred yet.
        if (cleanup)
        {
            await writer.WriteLineAsync("# Delete temporary file.");
            await writer.WriteLineAsync($"rm -f \"${{IN_FILE}}\"");
            await writer.WriteLineAsync();
        }
        else
            await writer.WriteLineAsync("# Input file cannot (necessarily) be deleted yet, since it is required for VPD estimation.");

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
    /// Check if a variable is expressed on a per-ground-area basis.
    /// </summary>
    /// <param name="units">The units of the variable.</param>
    /// <returns>True iff the variable is expressed on a per-ground-area basis.</returns>
    /// <remarks>This is used to decide whether to perform conservative remapping.</remarks>
    internal static bool HasPerAreaUnits(string units)
    {
        // Convert to lowercase and remove whitespace and periods for consistent matching
        units = units.ToLower().Replace(" ", "").Replace(".", "");

        // Match any of these patterns:
        // - m-2 or m^-2 (negative exponent notation)
        // - /m2 (division notation)
        return Regex.IsMatch(units, 
            @"(m\^?-2|/m2)");
    }

    /// <summary>
    /// Get an interpolation algorithm to be used when remapping the specified
    /// variable.
    /// </summary>
    /// <param name="info">Metadata for the variable in the dataset being processed.</param>
    /// <param name="variable">The variable to remap.</param>
    /// <returns>The interpolation algorithm to use.</returns>
    internal InterpolationAlgorithm GetInterpolationAlgorithm(VariableInfo info, ClimateVariable variable)
    {
        // Precipitation and shortwave radiation may require conservative
        // remapping, if they are NOT expressed on a per-ground-area basis.
        if (variable != ClimateVariable.Precipitation
            && variable != ClimateVariable.ShortwaveRadiation)
            return InterpolationAlgorithm.Bilinear;

        // Check if units are expressed on a per-ground-area basis.
        if (!HasPerAreaUnits(info.Units))
            // E.g. W
            // E.g. kg s-1
            return InterpolationAlgorithm.Conservative;

        // If, for example, precipitation is expressed in kg m-2 s-1, there's
        // no need for conservative remapping.
        return InterpolationAlgorithm.Bilinear;
    }

    /// <summary>
    /// Get the CDO remap operator to be used when remapping the specified
    /// variable.
    /// </summary>
    /// <param name="algorithm">The interpolation algorithm to use.</param>
    /// <returns>The CDO remap operator to use.</returns>
    /// <exception cref="ArgumentException"></exception>
    internal static string GetRemapOperator(InterpolationAlgorithm algorithm)
    {
        return algorithm switch
        {
            InterpolationAlgorithm.Bilinear => remapBilinear,
            InterpolationAlgorithm.Conservative => remapConservative,
            _ => throw new ArgumentException($"Unknown remap algorithm: {algorithm}")
        };
    }
}

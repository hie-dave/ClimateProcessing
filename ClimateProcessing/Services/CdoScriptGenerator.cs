using ClimateProcessing.Extensions;
using ClimateProcessing.Models;
using ClimateProcessing.Models.Options;
using ClimateProcessing.Units;

namespace ClimateProcessing.Services;

/// <summary>
/// Generates CDO commands for processing climate data.
/// </summary>
public class CdoScriptGenerator : IMergetimeScriptGenerator, IPreprocessingScriptGenerator
{
    /// <summary>
    /// Name of the variable containing the directory holding all input files
    /// used as operands for the remap command.
    /// </summary>
    protected const string inDirVariable = "IN_DIR";

    /// <summary>
    /// CDO's conservative remapping operator.
    /// </summary>
    private const string remapConservative = "remapcon";

    /// <summary>
    /// CDO's bilinear remapping operator.
    /// </summary>
    private const string remapBilinear = "remapbil";

    /// <summary>
    /// The name of the "standard name" attribute in the CF specification.
    /// </summary>
    private const string stdNameAttr = "standard_name";

    /// <summary>
    /// CDO operator which unpacks data (resolves add_offset and scale_factor).
    /// </summary>
    private const string unpackOperator = "-unpack";

    /// <summary>
    /// The name of the variable containing the directory holding all output
    /// files from the preprocessing step.
    /// </summary>
    private const string preprocessingOutDirVariable = "OUT_DIR";

    /// <summary>
    /// The name of the variable containing the path to the file holding the
    /// commands to be executed by nci-parallel.
    /// </summary>
    private const string commandsVar = "COMMANDS_FILE";

    /// <inheritdoc/>
    public async Task WriteMergetimeScriptAsync(IFileWriter writer, IMergetimeOptions options)
    {
        await writer.WriteLineAsync("# File paths.");
        await writer.WriteLineAsync($"{inDirVariable}=\"{options.InputDirectory.SanitiseBash()}\"");
        await writer.WriteLineAsync($"OUT_FILE=\"{options.OutputFile.SanitiseBash()}\"");
        await writer.WriteLineAsync();

        // The corrective operations read files from $IN_DIR and write to
        // $MOD_DIR, and then set IN_DIR=$MOD_DIR for the subsequent
        // mergetime operation. If no corrective operations are performed, then
        // IN_DIR still holds its original value.

        // Merge files and perform all operations in a single step.
        await writer.WriteLineAsync("log \"Merging files...\"");
        await writer.WriteLineAsync($"cdo {GetCommonArgs()} mergetime \"${{{inDirVariable}}}\"/*.nc \"${{OUT_FILE}}\"");
        await writer.WriteLineAsync("log \"All files merged successfully.\"");
        await writer.WriteLineAsync();

        // Remapped files are in jobfs, and will be automatically deleted upon
        // job completion (or failure), so no need for manual deletion.
    }

    /// <inheritdoc /> 
    public async Task WritePreprocessingScriptAsync(IFileWriter writer, IPreprocessingOptions options)
    {
        const string commandsFile = "${WORK_DIR}/commands.txt";

        string rename = GenerateRenameOperator(options.InputMetadata.Name, options.TargetMetadata.Name);
        string conversion = string.Join(" ", GenerateUnitConversionOperators(options.TargetMetadata.Name, options.InputMetadata.Units, options.TargetMetadata.Units, options.InputTimeStep));
        string aggregation = GenerateTimeAggregationOperator(options.InputTimeStep, options.OutputTimeStep, options.AggregationMethod);
        string remapOperator = GetRemapOperator(options.RemapAlgorithm);
        string remap = string.IsNullOrEmpty(options.GridFile) ? string.Empty : $"-{remapOperator},\"${{GRID_FILE}}\"";

        // We always have at least the unpack operator.
        string operators = $"{aggregation} {conversion} {rename} {unpackOperator} {remap}";
        operators = operators.CollapseWhitespace().Trim();

        await writer.WriteLineAsync("# File paths.");
        await writer.WriteLineAsync($"{inDirVariable}=\"{options.InputDirectory.SanitiseBash()}\"");
        await writer.WriteLineAsync($"{preprocessingOutDirVariable}=\"{options.OutputDirectory.SanitiseBash()}\"");
        if (!string.IsNullOrEmpty(options.GridFile))
            await writer.WriteLineAsync($"GRID_FILE=\"{options.GridFile.SanitiseBash()}\"");
        if (options.NCpus > 1)
            await writer.WriteLineAsync($"{commandsVar}=\"{commandsFile}\"");
        await writer.WriteLineAsync();

        // Create mod directory if needed.
        await writer.WriteLineAsync($"mkdir -p \"${{{preprocessingOutDirVariable}}}\"");
        await writer.WriteLineAsync();

        // The above operators all take a single file as input; therefore we
        // must perform them as a separate step to the mergetime.

        // Write description of processing steps.
        // FIXME: this won't appear directly above the commands when processing
        // narclim2 data which overrides the contents method.
        await writer.WriteLineAsync("# Perform corrective operations on input files:");
        if (!string.IsNullOrEmpty(remap))
            await writer.WriteLineAsync("# - Remap input files to target grid.");
        if (!string.IsNullOrEmpty(unpackOperator))
            await writer.WriteLineAsync("# - Unpack data.");
        if (!string.IsNullOrEmpty(rename))
            await writer.WriteLineAsync($"# - Rename variable from {options.InputMetadata.Name} to {options.TargetMetadata.Name}.");
        if (!string.IsNullOrEmpty(conversion))
            await writer.WriteLineAsync($"# - Convert units from {options.InputMetadata.Units} to {options.TargetMetadata.Units}.");
        if (!string.IsNullOrEmpty(aggregation))
            await writer.WriteLineAsync($"# - Aggregate data from {options.InputTimeStep} to {options.OutputTimeStep}.");

        await WritePreprocessingContentsAsync(writer, operators, options.Dataset, options.NCpus);
    }

    /// <summary>
    /// Writes the contents of the preprocessing script.
    /// </summary>
    /// <remarks>
    /// TODO: refactor the narclim2 preprocessing. Should probably become its
    /// own distinct preprocessor so that we don't have this clunky passing
    /// around of operators/dataset.
    /// </remarks>
    /// <param name="writer">The script writer.</param>
    /// <param name="operators">The operators to use.</param>
    /// <param name="dataset">The dataset to process.</param>
    /// <param name="ncpus">The number of CPUs to use for preprocessing.</param>
    protected virtual async Task WritePreprocessingContentsAsync(IFileWriter writer, string operators, IClimateDataset dataset, int ncpus)
    {
        if (ncpus == 1)
            await WriteSerialPreprocessingAsync(writer, operators);
        else
            await WriteParallelPreprocessingAsync(writer, operators, ncpus);
    }

    /// <summary>
    /// Writes the contents of the preprocessing script for a single CPU.
    /// </summary>
    /// <param name="writer">The script writer.</param>
    /// <param name="operators">The operators to use.</param>
    private static async Task WriteSerialPreprocessingAsync(IFileWriter writer, string operators)
    {
        await writer.WriteLineAsync($"for FILE in \"${{{inDirVariable}}}\"/*.nc");
        await writer.WriteLineAsync($"do");
        await writer.WriteLineAsync($"    cdo {GetCommonArgs()} {operators} \"${{FILE}}\" \"${{{preprocessingOutDirVariable}}}/$(basename \"${{FILE}}\")\"");
        await writer.WriteLineAsync("done");
    }

    /// <summary>
    /// Writes the contents of the preprocessing script for multiple CPUs.
    /// </summary>
    /// <param name="writer">The script writer.</param>
    /// <param name="operators">The operators to use.</param>
    /// <param name="ncpus">The number of CPUs to use.</param>
    private static async Task WriteParallelPreprocessingAsync(IFileWriter writer, string operators, int ncpus)
    {
        // We use the nci-parallel command, which is specific to the gadi HPC.
        // This command takes a --input-file <file> argument, which is a file
        // containing commands to be executed, one per line.

        // First, we need to generate the commands file. Iterate over all files
        // in the input directory, and generate the commands to preprocess them.
        await writer.WriteLineAsync($"# Generate commands file.");
        await writer.WriteLineAsync($"for FILE in \"${{{inDirVariable}}}\"/*.nc");
        await writer.WriteLineAsync($"do");
        await writer.WriteLineAsync($"    echo cdo {GetCommonArgs()} {operators} \"${{FILE}}\" \"${{{preprocessingOutDirVariable}}}/$(basename \"${{FILE}}\")\" >> \"${{{commandsVar}}}\"");
        await writer.WriteLineAsync("done");
        await writer.WriteLineAsync();

        await writer.WriteLineAsync("# Load additional modules required for parallel processing.");
        await writer.WriteLineAsync("module load openmpi nci-parallel");
        await writer.WriteLineAsync();

        await writer.WriteLineAsync(" # Pre-process files in parallel.");
        await writer.WriteLineAsync($"mpirun -n {ncpus} nci-parallel --input-file \"${{{commandsVar}}}\"");
    }

    /// <summary>
    /// Generates the operator to rename a variable.
    /// </summary>
    /// <param name="inName">The name of the input variable.</param>
    /// <param name="outName">The name of the output variable.</param>
    /// <returns>The CDO operator to use for renaming.</returns>
    internal static string GenerateRenameOperator(string inName, string outName)
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
    internal static IEnumerable<string> GenerateUnitConversionOperators(
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
            operators.Add(GetSetAttributeOperator(outputVar, "units", targetUnits));

        return operators;
    }

    /// <summary>
    /// Generate the operator to temporally aggregate the data.
    /// </summary>
    /// <param name="inputTimeStep">The input time step.</param>
    /// <param name="outputTimeStep">The output time step.</param>
    /// <param name="aggregationMethod">The aggregation method.</param>
    /// <returns>The CDO operator to use for temporal aggregation.</returns>
    internal static string GenerateTimeAggregationOperator(
        TimeStep inputTimeStep,
        TimeStep outputTimeStep,
        AggregationMethod aggregationMethod)
    {
        // Only aggregate if input and output timesteps differ
        if (inputTimeStep == outputTimeStep)
            return string.Empty;

        // Calculate the number of timesteps to aggregate
        int stepsToAggregate = outputTimeStep.Hours / inputTimeStep.Hours;

        var @operator = aggregationMethod.ToCdoOperator(outputTimeStep);

        return $"-{@operator},{stepsToAggregate}";
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

    /// <summary>
    /// Standard arguments used for all CDO invocations.
    /// TODO: make these configurable?
    /// </summary>
    internal static string GetCommonArgs()
    {
        return $"-L -O -v -z zip1";
    }

    /// <summary>
    /// Generates the operator to set an attribute of a variable.
    /// </summary>
    /// <param name="variable">The variable.</param>
    /// <param name="attribute">The attribute.</param>
    /// <param name="value">The value.</param>
    /// <returns>The CDO operator to set the attribute.</returns>
    internal static string GetSetAttributeOperator(string variable, string attribute, string value)
    {
        return $"-setattribute,'{variable}@{attribute}={value}'";
    }
}

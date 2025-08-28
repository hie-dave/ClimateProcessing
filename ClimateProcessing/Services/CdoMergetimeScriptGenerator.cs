using System.Text.RegularExpressions;
using ClimateProcessing.Extensions;
using ClimateProcessing.Models;
using ClimateProcessing.Models.Options;
using ClimateProcessing.Units;

namespace ClimateProcessing.Services;

/// <summary>
/// Generates CDO commands for processing climate data.
/// </summary>
public class CdoMergetimeScriptGenerator : IMergetimeScriptGenerator
{
    /// <summary>
    /// Name of the variable containing the directory holding all input files
    /// used as operands for the remap command.
    /// </summary>
    protected const string inDirVariable = "IN_DIR";

    /// <summary>
    /// Name of the variable containing the directory holding all modified input
    /// files. Modified means any combination of remapped, unpacked, renamed,
    /// aggregated, etc. This will not be used if no modifications are required.
    /// </summary>
    protected const string modDirVariable = "MOD_DIR";

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

    /// <inheritdoc/>
    public async Task WriteMergetimeScriptAsync(IFileWriter writer, IMergetimeOptions options)
    {
        string rename = GenerateRenameOperator(options.InputMetadata.Name, options.TargetMetadata.Name);
        string conversion = string.Join(" ", GenerateUnitConversionOperators(options.TargetMetadata.Name, options.InputMetadata.Units, options.TargetMetadata.Units, options.InputTimeStep));
        string aggregation = GenerateTimeAggregationOperator(options.InputTimeStep, options.OutputTimeStep, options.AggregationMethod);
        string unpack = "-unpack";
        string remapOperator = GetRemapOperator(options.RemapAlgorithm);
        string remap = string.IsNullOrEmpty(options.GridFile) ? string.Empty : $"-{remapOperator},\"${{GRID_FILE}}\"";

        string operators = $"{aggregation} {conversion} {rename} {unpack} {remap}";
        operators = Regex.Replace(operators, " +", " ").Trim();

        await writer.WriteLineAsync("# File paths.");
        await writer.WriteLineAsync($"{inDirVariable}=\"{options.InputDirectory.SanitiseBash()}\"");

        if (!string.IsNullOrWhiteSpace(operators))
            await writer.WriteLineAsync($"{modDirVariable}=\"${{WORK_DIR}}/mod\"");
        await writer.WriteLineAsync($"OUT_FILE=\"{options.OutputFile.SanitiseBash()}\"");
        if (!string.IsNullOrEmpty(options.GridFile))
            await writer.WriteLineAsync($"GRID_FILE=\"{options.GridFile.SanitiseBash()}\"");
        await writer.WriteLineAsync();

        // Create mod directory if needed.
        if (!string.IsNullOrWhiteSpace(operators))
        {
            await writer.WriteLineAsync($"mkdir -p \"${{{modDirVariable}}}\"");
            await writer.WriteLineAsync();
        }

        await WritePreMerge(writer, options);

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
                await writer.WriteLineAsync($"# - Rename variable from {options.InputMetadata.Name} to {options.TargetMetadata.Name}.");
            if (!string.IsNullOrEmpty(conversion))
                await writer.WriteLineAsync($"# - Convert units from {options.InputMetadata.Units} to {options.TargetMetadata.Units}.");
            if (!string.IsNullOrEmpty(aggregation))
                await writer.WriteLineAsync($"# - Aggregate data from {options.InputTimeStep} to {options.OutputTimeStep}.");

            await writer.WriteLineAsync($"for FILE in \"${{{inDirVariable}}}\"/*.nc");
            await writer.WriteLineAsync($"do");
            await writer.WriteLineAsync($"    cdo {GetCommonArgs()} {operators} \"${{FILE}}\" \"${{{modDirVariable}}}/$(basename \"${{FILE}}\")\"");
            await writer.WriteLineAsync("done");
            await writer.WriteLineAsync($"{inDirVariable}=\"${{{modDirVariable}}}\"");
            await writer.WriteLineAsync();
        }

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
    /// Write any pre-merge commands to the specified writer.
    /// </summary>
    /// <param name="writer">The text writer.</param>
    /// <param name="options">Options for the mergetime operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected virtual Task WritePreMerge(IFileWriter writer, IMergetimeOptions options)
    {
        return Task.CompletedTask;
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

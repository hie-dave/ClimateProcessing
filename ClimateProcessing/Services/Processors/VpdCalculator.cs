using ClimateProcessing.Extensions;
using ClimateProcessing.Models;

namespace ClimateProcessing.Services.Processors;

/// <summary>
/// Calculates VPD (Vapor Pressure Deficit) for a given dataset.
/// </summary>
public class VpdCalculator : IVariableProcessor
{
    /// <summary>
    /// The dependencies of the VPD calculation.
    /// </summary>
    private static readonly HashSet<ClimateVariableFormat> dependencies = new HashSet<ClimateVariableFormat>([
        ClimateVariableFormat.Timeseries(ClimateVariable.Temperature),
        ClimateVariableFormat.Timeseries(ClimateVariable.SpecificHumidity),
        ClimateVariableFormat.Timeseries(ClimateVariable.SurfacePressure),
    ]);

    /// <summary>
    /// The VPD estimation method to use.
    /// </summary>
    private readonly VPDMethod method;

    /// <inheritdoc/>
    public ClimateVariable TargetVariable => ClimateVariable.Vpd;

    /// <inheritdoc/>
    public ClimateVariableFormat OutputFormat => ClimateVariableFormat.Timeseries(TargetVariable);

    /// <inheritdoc/>
    public IEnumerable<ClimateVariableFormat> IntermediateOutputs => [];

    /// <inheritdoc/>
    public IReadOnlySet<ClimateVariableFormat> Dependencies => dependencies;

    /// <summary>
    /// Creates a new instance of the <see cref="VpdCalculator"/> class.
    /// </summary>
    /// <param name="method">The VPD estimation method to use.</param>
    public VpdCalculator(VPDMethod method)
    {
        this.method = method;
    }

    /// <summary>
    /// Generate a processing script for calculating the VPD for a dataset.
    /// </summary>
    /// <param name="dataset">The dataset.</param>
    /// <param name="pbsWriter">The PBS writer to use.</param>
    /// <returns>The script.</returns>
    public async Task<IReadOnlyList<Job>> CreateJobsAsync(
        IClimateDataset dataset,
        IJobCreationContext context)
    {
        string jobName = $"calc_vpd_{dataset.DatasetName}";
        using IFileWriter writer = context.FileWriterFactory.Create(jobName);

        string humidityFile = context.PathManager.GetDatasetFileName(dataset, ClimateVariable.SpecificHumidity, PathType.Working, context.VariableManager);
        string pressureFile = context.PathManager.GetDatasetFileName(dataset, ClimateVariable.SurfacePressure, PathType.Working, context.VariableManager);
        string temperatureFile = context.PathManager.GetDatasetFileName(dataset, ClimateVariable.Temperature, PathType.Working, context.VariableManager);

        //, context.VariableManager Generate an output file name.
        string outFile = GetUnoptimisedVpdOutputFilePath(context, dataset);

        // Equation file is written to JobFS, so it will never require a storage
        // directive.
        string[] requiredFiles = [
            humidityFile,
            pressureFile,
            temperatureFile,
            outFile,
        ];
        IEnumerable<PBSStorageDirective> storageDirectives = PBSStorageHelper.GetStorageDirectives(requiredFiles);

        await context.PBSLightweight.WritePBSHeader(writer, jobName, storageDirectives);

        await writer.WriteLineAsync("# File paths.");
        await writer.WriteLineAsync($"HUSS_FILE=\"{humidityFile}\"");

        await writer.WriteLineAsync($"PS_FILE=\"{pressureFile}\"");

        await writer.WriteLineAsync($"TAS_FILE=\"{temperatureFile}\"");

        string inFiles = "\"${HUSS_FILE}\" \"${PS_FILE}\" \"${TAS_FILE}\"";

        await writer.WriteLineAsync($"OUT_FILE=\"{outFile}\"");

        string eqnFile = "${WORK_DIR}/vpd_equations.txt";
        await writer.WriteLineAsync($"EQN_FILE=\"{eqnFile}\"");
        await writer.WriteLineAsync();

        await writer.WriteLineAsync("# Generate equation file.");
        await writer.WriteLineAsync("log \"Generating VPD equation file...\"");

        // Create equation file with selected method.
        await writer.WriteLineAsync($"cat >\"${{EQN_FILE}}\" <<EOF");
        await WriteVPDEquationsAsync(writer, dataset);
        await writer.WriteLineAsync("EOF");
        await writer.WriteLineAsync();

        // Calculate VPD using the equation file.
        await writer.WriteLineAsync("# Calculate VPD.");
        await writer.WriteLineAsync($"log \"Calculating VPD...\"");
        await writer.WriteLineAsync($"cdo {CdoMergetimeScriptGenerator.GetCommonArgs()} exprf,\"${{EQN_FILE}}\" -merge {inFiles} \"${{OUT_FILE}}\"");
        await writer.WriteLineAsync($"log \"VPD calculation completed successfully.\"");

        // We can't delete the intermediate files yet, because they are required
        // by the rechunk_X jobs, which may not have run yet.

        // Return the path to the generated script.
        return [new Job(
            jobName,
            writer.FilePath,
            ClimateVariableFormat.Timeseries(ClimateVariable.Vpd),
            outFile,
            Dependencies.Select(context.DependencyResolver.GetJob))];
    }

    /// <summary>
    /// Write the equations for estimating VPD to a text writer.
    /// </summary>
    /// <param name="writer">The writer to write to.</param>
    /// <param name="method">The VPD estimation method to use.</param>
    /// <param name="dataset">The dataset.</param>
    /// <exception cref="ArgumentException">If the specified VPD method is not supported.</exception>
    internal async Task WriteVPDEquationsAsync(IFileWriter writer, IClimateDataset dataset)
    {
        // All methods follow the same general pattern:
        // 1. Calculate saturation vapor pressure (_esat)
        // 2. Calculate actual vapor pressure (_e)
        // 3. Calculate VPD as (_esat - _e) / 1000 to convert to kPa

        string temp = dataset.GetVariableInfo(ClimateVariable.Temperature).Name;
        string huss = dataset.GetVariableInfo(ClimateVariable.SpecificHumidity).Name;
        string ps = dataset.GetVariableInfo(ClimateVariable.SurfacePressure).Name;

        // Other assumptions:
        // - Temperature is assumed to be in ℃. (TODO: dynamic equations based on actual tas output units)
        // - The name of the output variable is "vpd"
        var esatEquation = method switch
        {
            // Magnus equation (default)
            VPDMethod.Magnus => $"_esat=0.611*exp((17.27*{temp})/({temp}+237.3))*1000",

            // Buck (1981)
            // Buck's equation for temperatures above 0°C
            VPDMethod.Buck1981 => $"_esat=0.61121*exp((18.678-{temp}/234.5)*({temp}/(257.14+{temp})))*1000",

            // Alduchov and Eskridge (1996)
            // More accurate coefficients for the Magnus equation
            VPDMethod.AlduchovEskridge1996 => $"_esat=0.61094*exp((17.625*{temp})/({temp}+243.04))*1000",

            // Allen et al. (1998) FAO
            // Tetens equation with FAO coefficients
            VPDMethod.AllenFAO1998 => $"_esat=0.6108*exp((17.27*{temp})/({temp}+237.3))*1000",

            // Sonntag (1990)
            // Based on ITS-90 temperature scale
            VPDMethod.Sonntag1990 => $"_esat=0.61078*exp((17.08085*{temp})/({temp}+234.175+{temp}))*1000",

            _ => throw new ArgumentException($"Unsupported VPD calculation method: {method}")
        };

        await writer.WriteLineAsync($@"# Saturation vapor pressure (Pa) ({temp} in degC)");
        await writer.WriteLineAsync($"{esatEquation};");
        await writer.WriteLineAsync("# Actual vapor pressure (Pa)");
        await writer.WriteLineAsync($"_e=({huss}*{ps})/(0.622+0.378*{huss});");
        await writer.WriteLineAsync("# VPD (kPa)");
        await writer.WriteLineAsync("vpd=(_esat-_e)/1000;");
    }

    /// <summary>
    /// Given a temperature file, generate a file path for an equivalent file
    /// containing VPD data.
    /// </summary>
    /// <param name="dataset">The climate dataset.</param>
    /// <param name="temperatureFile">The temperature file.</param>
    /// <returns>The equivalent VPD file path.</returns>
    public static string GetVpdFilePath(IClimateDataset dataset, string temperatureFile)
    {
        // TODO: this could be simplified if VPD were a ClimateVariable.
        // Currently, these are the input variables though - so adding it would
        // require some refactoring, and would probably break the encapsulation
        // offered by this enum type, as most datasets *don't* have VPD as an
        // input variable.
        string fileName = Path.GetFileName(temperatureFile);
        string tempName = dataset.GetVariableInfo(ClimateVariable.Temperature).Name;
        string baseName = fileName.ReplaceFirst($"{tempName}_", "vpd_");
        string outFile = Path.Combine(Path.GetDirectoryName(temperatureFile)!, baseName);
        return outFile;
    }

    /// <summary>
    /// Get the path to the unoptimised VPD output file for a dataset.
    /// </summary>
    /// <param name="context">The job creation context.</param>
    /// <param name="dataset">The dataset.</param>
    /// <returns>The output file path.</returns>
    public string GetUnoptimisedVpdOutputFilePath(IJobCreationContext context, IClimateDataset dataset)
    {
        string temperatureFile = context.PathManager.GetDatasetFileName(dataset, ClimateVariable.Temperature, PathType.Working, context.VariableManager);
        return GetVpdFilePath(dataset, temperatureFile);
    }

    /// <summary>
    /// Check if the VPD calculation depends on the specified variable.
    /// </summary>
    /// <param name="variable">The variable.</param>
    /// <returns>True iff the VPD calculation depends on the specified variable.</returns>
    public static bool IsVpdDependency(ClimateVariable variable)
    {
        return variable == ClimateVariable.SpecificHumidity
            || variable == ClimateVariable.SurfacePressure
            || variable == ClimateVariable.Temperature;
    }
}

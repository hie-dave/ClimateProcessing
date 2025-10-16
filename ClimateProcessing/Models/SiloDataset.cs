using ClimateProcessing.Extensions;
using ClimateProcessing.Services;
using ClimateProcessing.Services.Processors;

namespace ClimateProcessing.Models;

/// <summary>
/// Encapsulates processing of the SILO dataset from CSIRO.
/// </summary>
public class SiloDataset : IClimateDataset
{
    /// <summary>
    /// Variable name and units as they exist in the SILO dataset.
    /// </summary>
    private static readonly Dictionary<ClimateVariable, (string name, string units)> variableMap = new()
    {
        { ClimateVariable.Precipitation, ("daily_rain", "mm") },
        { ClimateVariable.MaxTemperature, ("max_temp", "Celsius") },
        { ClimateVariable.MinTemperature, ("min_temp", "Celsius") },
        { ClimateVariable.ShortwaveRadiation, ("radiation", "Mj/m2") },
        { ClimateVariable.MaxRelativeHumidity, ("rh_tmax", "%") },
        { ClimateVariable.MinRelativeHumidity, ("rh_tmin", "%") }
    };

    /// <summary>
    /// The base path for all files in the dataset.
    /// </summary>
    private readonly string inputPath;

    /// <summary>
    /// Create a new instance of the SILO dataset.
    /// </summary>
    /// <param name="inputPath">Path to the SILO dataset.</param>
    public SiloDataset(string inputPath)
    {
        this.inputPath = inputPath;
    }

    /// <inheritdoc />
    public string DatasetName => "SILO";

    /// <inheritdoc />
    public string GenerateOutputFileName(ClimateVariable variable, VariableInfo metadata)
    {
        IEnumerable<string> files = GetInputFiles(variable);
        IEnumerable<int> years = files.Select(f => GetYearFromFileName(f)).ToList();
        int minYear = years.Min();
        int maxYear = years.Max();

        // Keep the same general format as the input file names.
        return $"{minYear}.{maxYear}.{metadata.Name}.nc";
    }

    /// <inheritdoc />
    public IEnumerable<string> GetInputFiles(ClimateVariable variable)
    {
        string directory = GetInputFilesDirectory(variable);
        return Directory.EnumerateFiles(directory, "*.nc");
    }

    /// <inheritdoc />
    public string GetInputFilesDirectory(ClimateVariable variable)
    {
        string variableName = GetVariableInfo(variable).Name;
        return Path.Combine(inputPath, variableName);
    }

    /// <inheritdoc />
    public string GetOutputDirectory()
    {
        // This path is relative to the configured output directory path.
        // In the case of SILO, all output files can go in the same directory,
        // because there are no GCM/RCM/etc options between which we need to
        // disambiguate the files.
        return ".";
    }

    /// <inheritdoc />
    public IEnumerable<IVariableProcessor> GetProcessors(IJobCreationContext context)
    {
        // Calculate mean daily temperature from min and max.
        string tempFileName = GenerateFileName(
            context,
            ClimateVariable.Temperature,
            ClimateVariable.MinTemperature);
        MeanProcessor tempCalculator = new MeanProcessor(
            tempFileName,
            ClimateVariable.Temperature,
            [ClimateVariable.MinTemperature, ClimateVariable.MaxTemperature]);

        // Calculate relative humidity from min and max.
        string relhumFileName = GenerateFileName(
            context,
            ClimateVariable.RelativeHumidity,
            ClimateVariable.MinRelativeHumidity);
        MeanProcessor relhumCalculator = new MeanProcessor(
            relhumFileName,
            ClimateVariable.RelativeHumidity,
            [ClimateVariable.MinRelativeHumidity, ClimateVariable.MaxRelativeHumidity]);

        return [
            new StandardVariableProcessor(ClimateVariable.Precipitation),
            new StandardVariableProcessor(ClimateVariable.ShortwaveRadiation),
            new StandardVariableProcessor(ClimateVariable.MinTemperature),
            new StandardVariableProcessor(ClimateVariable.MaxTemperature),
            new RechunkProcessorDecorator(tempCalculator),
            // No need to rechunk min and max rel. humidity, as it's only an
            // intermediate variable.
            new MergetimeProcessor(ClimateVariable.MinRelativeHumidity),
            new MergetimeProcessor(ClimateVariable.MaxRelativeHumidity),
            new RechunkProcessorDecorator(relhumCalculator),
            // air pressure not needed (as we have rel. humidity)
            // specific humidity not needed (as we have rel. humidity)
        ];
    }

    /// <inheritdoc />
    public VariableInfo GetVariableInfo(ClimateVariable variable)
    {
        if (!variableMap.TryGetValue(variable, out var info))
            throw new ArgumentException($"Variable {variable} not supported in SILO dataset");
        return new VariableInfo(info.name, info.units);
    }

    /// <summary>
    /// Get the year from a SILO filename.
    /// </summary>
    /// <param name="filename">The filename to parse.</param>
    /// <returns>The year.</returns>
    private static int GetYearFromFileName(string filename)
    {
        // YYYY.${variable}.nc
        string baseName = Path.GetFileNameWithoutExtension(filename);
        // TODO: is this locale dependent?
        return int.Parse(baseName.Split('.')[0]);
    }

    /// <summary>
    /// Generate a filename for a variable based on the output requirements.
    /// </summary>
    /// <param name="context">The job creation context.</param>
    /// <param name="variable">The variable to generate a filename for.</param>
    /// <param name="dependency">The variable that the filename is based on.</param>
    /// <returns>The filename.</returns>
    private string GenerateFileName(
        IJobCreationContext context,
        ClimateVariable variable,
        ClimateVariable dependency)
    {
        VariableInfo metadata = GetVariableInfo(dependency);
        string template = GenerateOutputFileName(dependency, metadata);
        template = Path.GetFileName(template);

        string varName = context.VariableManager.GetOutputRequirements(variable).Name;
        return template.ReplaceFirst(metadata.Name, varName);
    }
}

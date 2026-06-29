using System.Globalization;
using System.Text.RegularExpressions;
using ClimateProcessing.Models.Cmip6;
using ClimateProcessing.Services;
using ClimateProcessing.Services.Processors;

namespace ClimateProcessing.Models;

/*
FILE ORGANISATION
   /scratch/pt17/dh7190/cmip6/raw
    |-- <gcm>
        |-- <experiment>
            |-- <netcdf filename>
*/

/// <summary>
/// A CMIP6 dataset.
/// </summary>
/// <remarks>
/// Quick and dirty implementation of a dataset encapsulating some manually-
/// downloaded CMIP6 data. Not really intended for general use.
/// </remarks>
public partial class Cmip6Dataset : IClimateDataset
{
    /// <summary>
    /// Currently, only daily data is supported.
    /// </summary>
    const string Freq = "day";

    /// <summary>
    /// The base path to the CMIP6 dataset.
    /// </summary>
    private readonly string basePath;

    /// <summary>
    /// Initialises a new instance of the <see cref="Cmip6Dataset"/> class.
    /// </summary>
    /// <param name="basePath">The base path to the CMIP6 input data.</param>
    /// <param name="gcm">The GCM.</param>
    /// <param name="experiment">The experiment/scenario.</param>
    public Cmip6Dataset(string basePath, Cmip6Gcm gcm, Cmip6Experiment experiment)
    {
        this.basePath = basePath;
        Gcm = gcm;
        Experiment = experiment;
    }

    /// <summary>
    /// The variable name and units for each variable as provided by the
    /// CMIP6 dataset.
    /// </summary>
    private static readonly Dictionary<ClimateVariable, (string name, string units)> variables = new()
    {
        { ClimateVariable.RelativeHumidity, ("hurs", "%") },
        { ClimateVariable.Precipitation, ("pr", "kg m-2 s-1") },
        { ClimateVariable.ShortwaveRadiation, ("rsds", "W m-2") },
        { ClimateVariable.WindSpeed, ("sfcWind", "m s-1") },
        { ClimateVariable.SurfacePressure, ("ps", "Pa") },
        { ClimateVariable.MaxTemperature, ("tasmax", "K") },
        { ClimateVariable.MinTemperature, ("tasmin", "K") }
    };

    /// <summary>
    /// The GCM.
    /// </summary>
    public Cmip6Gcm Gcm { get; private init; }

    /// <summary>
    /// The experiment/scenario.
    /// </summary>
    public Cmip6Experiment Experiment { get; private init; }

    /// <inheritdoc />
    public string DatasetName => $"{Cmip6GcmExtensions.ToString(Gcm)}_{Cmip6ExperimentExtensions.ToString(Experiment)}";

    /// <inheritdoc />
    public string GenerateOutputFileName(ClimateVariable variable, VariableInfo metadata)
    {
        string variableName = metadata.Name;
        string gcmName = Cmip6GcmExtensions.ToString(Gcm);
        string exptName = Cmip6ExperimentExtensions.ToString(Experiment);

        List<string> inputFiles = GetInputFiles(variable).ToList();
        if (inputFiles.Count == 0)
            throw new ArgumentException($"No input files found for variable {variable}");

        DateTime startDate = GetDateFromFilename(inputFiles.First(), true);
        DateTime endDate = GetDateFromFilename(inputFiles.Last(), false);

        string startName = startDate.ToString("yyyyMM");
        string endName = endDate.ToString("yyyyMM");

        string variantName = ParseInputFiles(inputFiles, ParseVariantName, "variant");
        string gridName = ParseInputFiles(inputFiles, ParseGridName, "grid");

        // sfcWind_day_NorESM2-MM_ssp245_r1i1p1f1_gn_20710101-20801231.nc
        // <var>_<freq>_<gcm>_<expt>_<variant>_<grid>_<startdate>-<enddate>.nc
        return $"{variableName}_{Freq}_{gcmName}_{exptName}_{variantName}_{gridName}_{startName}-{endName}.nc";
    }

    /// <inheritdoc />
    public IEnumerable<string> GetInputFiles(ClimateVariable variable)
    {
        string dir = GetInputFilesDirectory(variable);
        if (!Directory.Exists(dir))
            return [];

        return Directory.GetFiles(dir, "*.nc").OrderBy(f => GetDateFromFilename(f, true));
    }

    /// <inheritdoc />
    public string GetInputFilesDirectory(ClimateVariable variable)
    {
        string gcm = Cmip6GcmExtensions.ToString(Gcm);
        string expt = Cmip6ExperimentExtensions.ToString(Experiment);
        string varName = GetVariableInfo(variable).Name;
        return Path.Combine(basePath, gcm, expt, varName);
    }

    /// <inheritdoc />
    public string GetOutputDirectory()
    {
        // This is a relative path, appended to base output directory.
        string gcm = Cmip6GcmExtensions.ToString(Gcm);
        string expt = Cmip6ExperimentExtensions.ToString(Experiment);
        return Path.Combine(gcm, expt);
    }

    /// <inheritdoc />
    public IEnumerable<IVariableProcessor> GetProcessors(IJobCreationContext context)
    {
        if (context.Config.OutputTimeStepHours != 24)
            throw new NotSupportedException("CMIP6 dataset only supports daily output");

        // No reason to return lazily, as this will always be enumerated.
        return variables.Keys.Select(v => new StandardVariableProcessor(v))
                             .ToList();
    }

    /// <inheritdoc />
    public VariableInfo GetVariableInfo(ClimateVariable variable)
    {
        if (!variables.TryGetValue(variable, out var info))
            throw new ArgumentException($"Variable {variable} not supported in CMIP6 dataset");

        return new VariableInfo(info.name, info.units);
    }

    /// <summary>
    /// Parse a property from a set of input files, ensuring that all input
    /// files have the same value for that property.
    /// </summary>
    /// <param name="inputFiles">The input file names to parse.</param>
    /// <param name="parseFunc">The function to parse the property from a filename.</param>
    /// <param name="propertyName">The name of the property being parsed.</param>
    /// <returns>The parsed property value.</returns>
    /// <exception cref="ArgumentException">If the input files have inconsistent values for the property.</exception>
    private string ParseInputFiles(IEnumerable<string> inputFiles, Func<string, string> parseFunc, string propertyName)
    {
        IEnumerable<string> parsed = inputFiles.Select(parseFunc).Distinct();
        if (parsed.Count() > 1)
            throw new ArgumentException($"Input files have inconsistent values for the parsed property: {propertyName}: {string.Join(", ", parsed)}");
        return parsed.FirstOrDefault() ?? throw new ArgumentException("No input files provided");
    }

    /// <summary>
    /// Parse the variant name from a CMIP6 filename. The variant name is the
    /// 5th underscore-separated part of the filename (e.g. "r1i1p1f1").
    /// </summary>
    /// <param name="file">The filename to parse.</param>
    /// <returns>The variant name.</returns>
    /// <exception cref="ArgumentException">If the filename is invalid.</exception>
    private string ParseVariantName(string file)
    {
        string basename = Path.GetFileNameWithoutExtension(file);
        string[] parts = basename.Split('_');
        if (parts.Length < 6)
            throw new ArgumentException($"Invalid CMIP6 filename: {file}");
        return parts[4];
    }

    /// <summary>
    /// Parse the grid name from a CMIP6 filename. The grid name is the 6th
    /// underscore-separated part of the filename (e.g. "gn").
    /// </summary>
    /// <param name="file">The filename to parse.</param>
    /// <returns>The grid name.</returns>
    /// <exception cref="ArgumentException">If the filename is invalid.</exception>
    private string ParseGridName(string file)
    {
        string basename = Path.GetFileNameWithoutExtension(file);
        string[] parts = basename.Split('_');
        if (parts.Length < 6)
            throw new ArgumentException($"Invalid CMIP6 filename: {file}");
        return parts[5];
    }

    /// <summary>
    /// Gets the start or end date from a CMIP6 daily filename.
    /// </summary>
    /// <param name="fileName">The filename to parse.</param>
    /// <param name="start">If true, get the start date. If false, get the end date.</param>
    /// <returns>The date.</returns>
    /// <exception cref="ArgumentException">If the date cannot be determined from the filename.</exception>
    private static DateTime GetDateFromFilename(string fileName, bool start)
    {
        // Example filename: ta10_AUS-11_ERA5_historical_hres_BOM_BARRA-R2_v1_mon_202507-202507.nc

        // Remove the directory component, if one is present.
        fileName = Path.GetFileName(fileName);

        // Parse the filename.
        Match match = DailyFilenameDateRegex().Match(fileName);
        if (!match.Success)
            throw new ArgumentException($"Unable to get date from filename. Invalid filename format: {fileName}");

        // Choose the correct group and format, depending on whether we are
        // looking for the start or end date.
        string dateStr = match.Groups[start ? 1 : 2].Value;

        // Parse the date.
        return DateTime.ParseExact(dateStr, "yyyyMMdd", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Regular expression which parses a CMIP6 daily file name. Contains two
    /// capturing groups, for the start and end dates (YYYYMMDD), respectively.
    /// </summary>
    [GeneratedRegex(@"_(\d{8})-(\d{8})\.nc$")]
    private static partial Regex DailyFilenameDateRegex();
}

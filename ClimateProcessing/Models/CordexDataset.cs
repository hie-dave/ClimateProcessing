
using System.Globalization;
using System.Text.RegularExpressions;
using ClimateProcessing.Extensions;
using ClimateProcessing.Models.Cordex;
using ClimateProcessing.Services;
using ClimateProcessing.Services.Processors;

namespace ClimateProcessing.Models;

/// <summary>
/// CORDEX-CMIP6-based regridded and calibrated data for Australia.
/// NCI project: kj66
/// </summary>
/// <remarks>
/// FILE ORGANISATION
/// 
///    /g/data/kj66/CORDEX/
///       |-- <product>
///          |-- <mip_era>
///             |-- <activity_id>
///                |-- <domain_id>
///                   |-- <institution_id>
///                      |-- <driving_source_id>
///                         |-- <driving_experiment_id>
///                            |-- <driving_variant_label>
///                               |-- <source_id>
///                                   |-- <version_realisation>
///                                      |-- <freq>
///                                         |-- <variable_id>
///                                            |-- <version>
///                                               |-- <netcdf_filename>
///    where,
///      <product> is:
///          "output" (for all regridded data)
///          "outputAdjust" (for all bias corrected/adjusted data)
///      <mip-era> is "CMIP6"
///      <activity_id> is:
///          "DD" for dynamical downscaling (i.e. the regridded data)
///          "bias-adjusted-output" (for the bias corrected/adjusted data) 
///      <domain_id> is the spatial domain and grid resolution: "AUST-05i"
///      <institution_id> is the institution that performed the downscaling:
///          "BOM" (Bureau of Meteorology)
///          "CSIRO" (Commonwealth Scientific and Industrial Research Organisation) 
///      <driving_source_id> is the global driving model that was downscaled:
///           "ACCESS-CM2", "ACCESS-ESM1-5", "CESM2", "CMCC-ESM2", "EC-Earth3",
///           "MPI-ESM1-2-HR", "NorESM2-MM"
///      <driving_experiment_id> is "historical", "ssp126" or "ssp370"
///      <driving_variant_label> labels the ensemble member of the CMIP6 simulation 
///            that produced forcing data.
///      <source_id> is the regional climate model that performed the downscaling:
///            "BARPA-R" or "CCAM-v2203-SN"
///      <version_realisation> identifies the modelling version
///      <freq> is "day" (daily)
///      <variable_id> is the variable name:
///          "hursmax" (daily maximum surface relative humidity)
///          "hursmin" (daily minimum surface relative humidity)
///          "pr" (precipitation)
///          "rsds" (surface downwelling solar radiation)
///          "sfcWindmax" (daily maximum surface wind speed)
///          "tasmax" (daily maximum surface air temperature)
///          "tasmin" (daily minimum surface air temperature)
///      <version> denotes the date of data generation or date of data release
///          in the form "vYYYYMMDD"
///      <netcdf_filename> is: 
///           <variable_id>_<domain_id>_<driving_source_id>_<driving_experiment_id>_
///           <driving_variant_label>_<institution_id>_<source_id>_
///           <version_realisation>_<freq>[_<StartTime>-<EndTime>].nc
/// 
///    An example for the reridded data is, 
///       /g/data/kj66/CORDEX/output/CMIP6/DD/AUST-05i/BOM/EC-Earth3/historical/
///       r1i1p1f1/BARPA-R/v1-r1/day/sfcWindmax/v20241216/sfcWindmax_AUST-05i_
///       EC-Earth3_historical_r1i1p1f1_BOM_BARPA-R_v1-r1_day_19600101-19601231.nc
/// 
///    An example for the bias corrected data is,
///       /g/data/kj66/CORDEX/output-Adjust/CMIP6/bias-adjusted-output/AUST-05i/
///       CSIRO/CNRM-ESM2-1/ssp370/r1i1p1f2/CCAM-v2203-SN/
///       v1-r1-ACS-QME-BARRAR2-1980-2022/day/tasminAdjust/v20241216/tasminAdjust_
///       AUST-05i_CNRM-ESM2-1_ssp370_r1i1p1f2_CSIRO_CCAM-v2203-SN_
///       v1-r1-ACS-QME-BARRAR2-1980-2022_day_20660101-20661231.nc
/// </remarks>
public class CordexDataset : IClimateDataset
{
    /// <summary>
    /// The variables in the dataset.
    /// </summary>
    private static readonly IReadOnlyDictionary<ClimateVariable, (string name, string units)> inputVariables = new Dictionary<ClimateVariable, (string name, string units)>()
    {
        // Dataset does not contain specific humidity.
        // Dataset does not contain Surface pressure.

        // Surface downwelling shortwave radiation
        { ClimateVariable.ShortwaveRadiation, ("rsds", "W m-2") },

        // Near-surface wind speed
        { ClimateVariable.WindSpeed, ("sfcWindmax", "m s-1") },

        // Dataset contains only min and max temperature.

        // Precipitation
        { ClimateVariable.Precipitation, ("pr", "mm d-1") },

        // Daily maximum near-surface air temperature
        { ClimateVariable.MaxTemperature, ("tasmax", "degC") },

        // Daily minimum near-surface air temperature
        { ClimateVariable.MinTemperature, ("tasmin", "degC") },

        // Dataset contains min and max relative humidity only.
        { ClimateVariable.MinRelativeHumidity, ("hursmin", "%") },
        { ClimateVariable.MaxRelativeHumidity, ("hursmax", "%") },
    };

    /// <summary>
    /// The base path for all files in the dataset.
    /// </summary>
    private readonly string basePath;

    /// <summary>
    /// The product of the dataset.
    /// </summary>
    /// <remarks>
    /// product is:
    ///     "output" (for all regridded data)
    ///     "outputAdjust" (for all bias corrected/adjusted data)
    /// That said, only the "output" directory exists at the current time.
    /// </remarks>
    private const string product = "output";

    /// <summary>
    /// The era of the dataset.
    /// </summary>
    /// <remarks>
    /// There's currently only one valid era, CMIP6.
    /// </remarks>
    private const CordexEra era = CordexEra.CMIP6;

    /// <summary>
    /// The activity of the dataset.
    /// </summary>
    private readonly CordexActivity activity;

    /// <summary>
    /// The domain of the dataset.
    /// </summary>
    private readonly CordexDomain domain;

    /// <summary>
    /// The institution owning the dataset.
    /// </summary>
    private readonly CordexInstitution institution;

    /// <summary>
    /// The GCM of the dataset.
    /// </summary>
    private readonly CordexGcm gcm;

    /// <summary>
    /// The experiment of the dataset.
    /// </summary>
    private readonly CordexExperiment experiment;

    /// <summary>
    /// The source of the dataset.
    /// </summary>
    private readonly CordexSource source;

    /// <summary>
    /// The version realisation of the CORDEX data.
    /// </summary>
    private readonly CordexVersion versionRealisation;

    /// <summary>
    /// The frequency of the dataset.
    /// </summary>
    /// <remarks>
    /// There's currently only one valid frequency, daily.
    /// </remarks>
    private const CordexFrequency frequency = CordexFrequency.Daily;

    /// <summary>
    /// The version of the dataset.
    /// </summary>
    private const string version = "latest";

    /// <summary>
    /// Create a new instance of the CORDEX-CMIP6 dataset.
    /// </summary>
    /// <param name="basePath">Base path to the CORDEX-CMIP6 dataset.</param>
    /// <param name="activity">The activity of the dataset.</param>
    /// <param name="domain">The domain of the dataset.</param>
    /// <param name="institution">The institution of the dataset.</param>
    /// <param name="gcm">The GCM of the dataset.</param>
    /// <param name="experiment">The experiment of the dataset.</param>
    /// <param name="source">The source of the dataset.</param>
    public CordexDataset(
        string basePath,
        CordexActivity activity,
        CordexDomain domain,
        CordexInstitution institution,
        CordexGcm gcm,
        CordexExperiment experiment,
        CordexSource source,
        CordexVersion version)
    {
        this.basePath = basePath;
        this.activity = activity;
        this.domain = domain;
        this.institution = institution;
        this.gcm = gcm;
        this.experiment = experiment;
        this.source = source;
        versionRealisation = version;
    }

    /// <inheritdoc /> 
    public string DatasetName => $"{domain.ToDomainId()}_{gcm.ToGcmId()}_{experiment.ToExperimentId()}_{gcm.GetVariantLabel()}_{institution.ToInstitutionId()}_{source.ToSourceId()}_{versionRealisation}_{frequency.ToFrequencyId()}";

    /// <inheritdoc />
    public string GenerateOutputFileName(ClimateVariable variable)
    {
        // Get the first input file for this variable to use as a pattern.
        List<string> inputFiles = GetInputFiles(variable).ToList();

        if (inputFiles.Count == 0)
            throw new InvalidOperationException($"No input files found for variable {GetVariableInfo(variable).Name} in domain {domain}, GCM {gcm}, experiment {experiment}. Expected path:\n{GetInputFilesDirectory(variable)}");

        DateTime startDate = GetDateFromFilename(inputFiles.First(), true);
        DateTime endDate = GetDateFromFilename(inputFiles.Last(), false);

        // Use the file name of the first input file as a template.
        string firstFile = Path.GetFileName(inputFiles.First());

        // Extract the pattern before the date range.
        // rsds_AUST-05i_ACCESS-CM2_historical_r4i1p1f1_BOM_BARPA-R_v1-r1_day_19600101-19601231.nc
        string prefix = string.Join("_", firstFile.Split('_').TakeWhile(p => !p.Contains(".nc")));

        // Add the date range and extension.
        return $"{prefix}_{startDate:yyyyMMdd}-{endDate:yyyyMMdd}.nc";
    }

    /// <inheritdoc />
    public IEnumerable<string> GetInputFiles(ClimateVariable variable)
    {
        string dir = GetInputFilesDirectory(variable);
        if (!Directory.Exists(dir))
            return Enumerable.Empty<string>();

        return Directory.GetFiles(dir, "*.nc").OrderBy(f => GetDateFromFilename(f, true));
    }

    /// <inheritdoc />
    public string GetInputFilesDirectory(ClimateVariable variable)
    {
        return Path.Combine(
            basePath,
            product,
            era.ToEraId(),
            activity.ToActivityId(),
            domain.ToDomainId(),
            institution.ToInstitutionId(),
            gcm.ToGcmId(),
            experiment.ToExperimentId(),
            gcm.GetVariantLabel(),
            source.ToSourceId(),
            versionRealisation.ToVersionId(),
            frequency.ToFrequencyId(),
            GetVariableInfo(variable).Name,
            version);
    }

    /// <inheritdoc />
    public string GetOutputDirectory()
    {
        return Path.Combine(
            gcm.ToGcmId(),
            experiment.ToExperimentId(),
            activity.ToActivityId(),
            institution.ToInstitutionId(),
            source.ToSourceId());
    }

    /// <inheritdoc />
    public VariableInfo GetVariableInfo(ClimateVariable variable)
    {
        if (!inputVariables.TryGetValue(variable, out var info))
            throw new ArgumentException($"Variable {variable} not supported in CORDEX dataset");
        return new VariableInfo(info.name, info.units);
    }

    /// <summary>
    /// Gets the start or end date from a NARCliM2 filename.
    /// </summary>
    /// <param name="filename">The filename to parse.</param>
    /// <param name="start">If true, get the start date. If false, get the end date.</param>
    /// <returns>The date.</returns>
    /// <exception cref="ArgumentException">If the date cannot be determined from the filename.</exception>
    private DateTime GetDateFromFilename(string filename, bool start)
    {
        // Example filename: tas_AUS18_ACCESS-ESM1-5_historical_r6i1p1f1_NSW-Government_NARCliM2-0-WRF412R3_v1-r1_mon_195101-195112.nc
        // Remove the directory component, if one is present.
        filename = Path.GetFileName(filename);

        // Determine if the file is subdaily and use the correct regex.
        // Note: 1hr and 3hr files contain yyyyMMddHH, but day, mon, ..., etc
        //       use yyyyMMdd format (ie not time component).
        (string regex, string fmt) = GetDateRegexFormat();

        // Parse the filename.
        Match match = Regex.Match(filename, regex);
        if (!match.Success)
            throw new ArgumentException($"Unable to get date from filename. Invalid filename format: {filename}");

        // Choose the correct group and format, depending on whether we are
        // looking for the start or end date.
        string dateStr = match.Groups[start ? 1 : 2].Value;

        // Parse the date.
        return DateTime.ParseExact(dateStr, fmt, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Get the regular expression and managed DateTime format string which
    /// can be used to parse NARCliM2 filenames for files on the specified
    /// frequency.
    /// </summary>
    /// <param name="frequency">The frequency of the filename.</param>
    /// <returns>Tuple of regular expression and DateTime format string.</returns>
    /// <exception cref="ArgumentException">Thrown when an invalid frequency is specified. Should never happen in practice unless narclim add more timestep frequencies in the future (e.g. 6hr).</exception>
    private (string regex, string format) GetDateRegexFormat()
    {
        const string regexDay = @"_(\d{8})-(\d{8})\.nc$";
        const string formatDay = "yyyyMMdd";

        return frequency switch
        {
            CordexFrequency.Daily => (regexDay, formatDay),
            _ => throw new ArgumentException($"Unknown frequency: {frequency}")
        };
    }

    /// <inheritdoc />
    public IEnumerable<IVariableProcessor> GetProcessors(IJobCreationContext context)
    {
        if (context.Config.Version == ModelVersion.Dave)
            throw new NotSupportedException($"Dataset {DatasetName} is not supported in version {context.Config.Version}");

        // Calculate temperature from min and max.
        IEnumerable<ClimateVariable> tempDeps = [ClimateVariable.MinTemperature, ClimateVariable.MaxTemperature];
        string tempFileName = GenerateFileName(context, ClimateVariable.Temperature, ClimateVariable.MinTemperature, PathType.Working);
        MeanProcessor tempCalculator = new MeanProcessor(tempFileName, ClimateVariable.Temperature, tempDeps);

        // Calculate relative humidity from min and max.
        MeanProcessor relhumCalculator = new MeanProcessor(
            GenerateFileName(context, ClimateVariable.RelativeHumidity, ClimateVariable.MinRelativeHumidity, PathType.Working),
            ClimateVariable.RelativeHumidity,
            [ClimateVariable.MinRelativeHumidity, ClimateVariable.MaxRelativeHumidity]);

        return [
            new StandardVariableProcessor(ClimateVariable.Precipitation),
            new StandardVariableProcessor(ClimateVariable.ShortwaveRadiation),
            new StandardVariableProcessor(ClimateVariable.WindSpeed),
            new StandardVariableProcessor(ClimateVariable.MinTemperature),
            new StandardVariableProcessor(ClimateVariable.MaxTemperature),
            new RechunkProcessorDecorator(tempCalculator),
            new StandardVariableProcessor(ClimateVariable.MinRelativeHumidity),
            new StandardVariableProcessor(ClimateVariable.MaxRelativeHumidity),
            new RechunkProcessorDecorator(relhumCalculator),
            // air pressure not needed (as we have rel. humidity)
            // specific humidity not needed (as we have rel. humidity)
        ];
    }

    private string GenerateFileName(
        IJobCreationContext context,
        ClimateVariable variable,
        ClimateVariable dependency,
        PathType pathType)
    {
        string template = context.PathManager.GetDatasetFileName(this, dependency, pathType);
        template = Path.GetFileName(template);

        string varName = context.VariableManager.GetOutputRequirements(variable).Name;
        string depName = context.VariableManager.GetOutputRequirements(dependency).Name;
        return template.ReplaceFirst($"{depName}_", $"{varName}_");
    }
}

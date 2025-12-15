using System.Globalization;
using System.Text.RegularExpressions;
using ClimateProcessing.Models.Barra2;
using ClimateProcessing.Services;
using ClimateProcessing.Services.Processors;

namespace ClimateProcessing.Models;

/*
FILE ORGANISATION
   /g/data/ob53
   |-- <product>
     |-- <nature of data>
             |-- <activity_id>
                  |-- <domain_id>
                       |-- <RCM-institution_id>
                            |-- <driving_source_id>
                                 |-- <driving_experiment_id>
                                      |-- <driving_variant_label>
                                           |-- <source_id>
                                                |-- <version_realisation>
                                                     |-- <freq>
                                                          |-- <variable_id>
                                                              |-- <version>
                                                                   |-- <netcdf filename>

   where,
     <product> is BARRA2
     <nature of data> is output, referring to model output
     <activity_id> is reanalysis
     <domain_id> is spatial domain and grid resolution of the data, namely 
               AUS-11, AUS-22, AUST-04, AUST-11
     <RCM-institution> is BOM
     <driving_source_id> is ERA5 that drives BARRA2 at the lateral boundary
     <driving_experiment_id> is historical
     <driving_variant_label> labels the nature of ERA5 data used, either hres 
               or eda
     <source_id> is BARRA-R2, BARRA-RE2, or BARRA-C2, refer to 
               Extended Documentation
     <version_realisation> is v1, identifies the modelling version of BARRA2
     <freq> is the time frequency of the data: 1hr (1-hourly), 3hr, 6hr, 
               day (daily), mon (monthly), fx (constant)
     <variable_id> is the variable name, mostly based on,
               https://docs.google.com/spreadsheets/d/1qUauozwXkq7r1g-L4ALMIkCNINIhhCPx/edit#gid=1672965248
     <version> denotes the date of data generation or date of data release
               or 'latest' points to the latest version.
     <netcdf filename> is
               <variable_id>_<domain_id>_<driving_source_id>_<driving_experiment_id>_<driving_variant_label>_<RCM-institution_id>_<source_id>_<version_realisation>_<freq>[_<StartTime>-<EndTime>].nc

  Example:
    /g/data/ob53/BARRA2/output/reanalysis/AUS-11/BOM/ERA5/historical/hres/BARRA-R2/v1/mon/ua100m/v20231001/ua100m_AUS-11_ERA5_historical_hres_BOM_BARRA-R2_v1_mon_197901-197901.nc
    /g/data/ob53/BARRA2/output/reanalysis/AUS-22/BOM/ERA5/historical/eda/BARRA-RE2/v1/1hr/tas/v20231001/tas_AUS-22_ERA5_historical_eda_BOM_BARRA-RE2_v1_1hr_202201-202201.nc
*/

public partial class Barra2Dataset : IClimateDataset
{
    /// <summary>
    /// The version of the BARRA2 dataset.
    /// </summary>
    private const string version = "v1";

    /// <summary>
    /// The latest processing version.
    /// </summary>
    private const string latestProcessing = "latest";

    /// <summary>
    /// The ID of the source that drives BARRA2 at the lateral boundary.
    /// </summary>
    private const string drivingSourceID = "ERA5";

    /// <summary>
    /// The experiment ID of the driving source.
    /// </summary>
    private const string drivingExperimentID = "historical";

    /// <summary>
    /// The institution ID of the driving source.
    /// </summary>
    private const string drivingInstitutionID = "BOM";

    /// <summary>
    /// The variable name and units for each variable as provided by the
    /// BARRA2 dataset.
    /// </summary>
    private static readonly Dictionary<ClimateVariable, (string name, string units)> variableMap = new()
    {
        { ClimateVariable.SpecificHumidity, ("huss", "1") },
        { ClimateVariable.SurfacePressure, ("ps", "Pa") },
        { ClimateVariable.ShortwaveRadiation, ("rsds", "W m-2") },
        { ClimateVariable.WindSpeed, ("sfcWind", "m s-1") },
        { ClimateVariable.Temperature, ("tas", "K") },
        { ClimateVariable.Precipitation, ("pr", "kg m-2 s-1") },
        { ClimateVariable.MinTemperature, ("tasmin", "K") },
        { ClimateVariable.MaxTemperature, ("tasmax", "K") },
    };

    /// <summary>
    /// The base path to the BARRA2 dataset.
    /// </summary>
    private readonly string basePath;

    /// <summary>
    /// The domain of the BARRA2 dataset.
    /// </summary>
    private readonly Barra2Domain domain;

    /// <summary>
    /// The frequency of the BARRA2 dataset.
    /// </summary>
    private readonly Barra2Frequency frequency;

    /// <summary>
    /// The grid of the BARRA2 dataset.
    /// </summary>
    private readonly Barra2Grid grid;

    /// <summary>
    /// The variant of the BARRA2 dataset.
    /// </summary>
    private readonly Barra2Variant variant;

    /// <summary>
    /// Initializes a new instance of the <see cref="Barra2Dataset"/> class.
    /// </summary>
    /// <param name="basePath">The base path to the BARRA2 dataset.</param>
    /// <param name="domain">The domain of the BARRA2 dataset.</param>
    /// <param name="frequency">The frequency of the BARRA2 dataset.</param>
    /// <param name="grid">The grid of the BARRA2 dataset.</param>
    /// <param name="variant">The variant of the BARRA2 dataset.</param>
    public Barra2Dataset(string basePath, Barra2Domain domain, Barra2Frequency frequency, Barra2Grid grid, Barra2Variant variant)
    {
        this.basePath = basePath;
        this.domain = domain;
        this.frequency = frequency;
        this.grid = grid;
        this.variant = variant;
    }

    /// <summary>
    /// The name of the dataset.
    /// </summary>
    public string DatasetName => $"{Barra2DomainExtensions.ToString(domain)}_{drivingSourceID}_{drivingExperimentID}_{Barra2VariantExtensions.ToString(variant)}_BOM_{Barra2GridExtensions.ToString(grid)}_{version}_{Barra2FrequencyExtensions.ToString(frequency)}";

    /// <inheritdoc/>
    public string GenerateOutputFileName(ClimateVariable variable, VariableInfo metadata)
    {
        string variableName = metadata.Name;
        string domainName = Barra2DomainExtensions.ToString(domain);
        string gridName = Barra2GridExtensions.ToString(grid);
        string variantName = Barra2VariantExtensions.ToString(variant);
        string frequencyName = Barra2FrequencyExtensions.ToString(frequency);

        List<string> inputFiles = GetInputFiles(variable).ToList();
        if (inputFiles.Count == 0)
            throw new ArgumentException($"No input files found for variable {variable}");

        DateTime startDate = GetDateFromFilename(inputFiles.First(), true);
        DateTime endDate = GetDateFromFilename(inputFiles.Last(), false);

        string startName = startDate.ToString("yyyyMM");
        string endName = endDate.ToString("yyyyMM");

        // tas_AUS-11_ERA5_historical_hres_BOM_BARRA-R2_v1_1hr_202508-202508.nc
        // <variable_id>_<domain_id>_<driving_source_id>_<driving_experiment_id>_<driving_variant_label>_<RCM-institution_id>_<source_id>_<version_realisation>_<freq>[_<StartTime>-<EndTime>].nc
        return $"{variableName}_{domainName}_{drivingSourceID}_{drivingExperimentID}_{variantName}_{drivingInstitutionID}_{gridName}_{version}_{frequencyName}_{startName}-{endName}.nc";
    }

    /// <inheritdoc/>
    public IEnumerable<string> GetInputFiles(ClimateVariable variable)
    {
        string dir = GetInputFilesDirectory(variable);
        if (!Directory.Exists(dir))
            return [];

        return Directory.GetFiles(dir, "*.nc").OrderBy(f => GetDateFromFilename(f, true));
    }

    /// <inheritdoc/>
    public string GetInputFilesDirectory(ClimateVariable variable)
    {
        return Path.Combine(
            basePath,
            "BARRA2",
            "output",
            "reanalysis",
            Barra2DomainExtensions.ToString(domain),
            drivingInstitutionID,
            drivingSourceID,
            "historical",
            Barra2VariantExtensions.ToString(variant),
            Barra2GridExtensions.ToString(grid),
            version,
            Barra2FrequencyExtensions.ToString(frequency),
            GetVariableInfo(variable).Name,
            latestProcessing
        );
    }

    /// <inheritdoc/>
    public string GetOutputDirectory()
    {
        string domain = Barra2DomainExtensions.ToString(this.domain);
        string grid = Barra2GridExtensions.ToString(this.grid);
        string variant = Barra2VariantExtensions.ToString(this.variant);
        string frequency = Barra2FrequencyExtensions.ToString(this.frequency);

        return Path.Combine(domain, grid, variant, frequency);
    }

    /// <inheritdoc/>
    public IEnumerable<IVariableProcessor> GetProcessors(IJobCreationContext context)
    {
        List<ClimateVariable> standardVariables = [
            ClimateVariable.Temperature,
            ClimateVariable.Precipitation,
            ClimateVariable.SpecificHumidity,
            ClimateVariable.SurfacePressure,
            ClimateVariable.ShortwaveRadiation,
            ClimateVariable.WindSpeed
        ];
        if (context.Config.OutputTimeStep.Hours == 24)
        {
            standardVariables.Add(ClimateVariable.MinTemperature);
            standardVariables.Add(ClimateVariable.MaxTemperature);
        }

        List<IVariableProcessor> processors = standardVariables
            .Select(v => new StandardVariableProcessor(v))
            .ToList<IVariableProcessor>();
        if (context.Config.Version == ModelVersion.Dave)
            processors.Add(new RechunkProcessorDecorator(new VpdCalculator(context.Config.VPDMethod)));
        return processors;
    }

    /// <inheritdoc/>
    public VariableInfo GetVariableInfo(ClimateVariable variable)
    {
        // BARRA2 provides tas (surface air temperature) only at hourly
        // frequency.
        if (variable == ClimateVariable.Temperature && frequency != Barra2Frequency.Hour1)
            throw new ArgumentException($"Variable {variable} not supported with frequency {frequency} (only 1hr is supported)");

        if (!variableMap.TryGetValue(variable, out var info))
            throw new ArgumentException($"Variable {variable} not supported in BARRA2 dataset");

        return new VariableInfo(info.name, info.units);
    }

    /// <summary>
    /// Gets the start or end date from a BARRA2 filename.
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
        Match match = FilenameDateRegex().Match(fileName);
        if (!match.Success)
            throw new ArgumentException($"Unable to get date from filename. Invalid filename format: {fileName}");

        // Choose the correct group and format, depending on whether we are
        // looking for the start or end date.
        string dateStr = match.Groups[start ? 1 : 2].Value;

        // Parse the date.
        return DateTime.ParseExact(dateStr, "yyyyMM", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Regular expression which parses a BARRA2 file name. Contains two
    /// capturing groups, for the start and end dates (YYYYMM), respectively.
    /// </summary>
    [GeneratedRegex(@"_(\d{6})-(\d{6})\.nc$")]
    private static partial Regex FilenameDateRegex();
}

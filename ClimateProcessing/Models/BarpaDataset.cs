using System.Globalization;
using System.Text.RegularExpressions;
using ClimateProcessing.Models.Barpa;
using ClimateProcessing.Services;
using ClimateProcessing.Services.Processors;

namespace ClimateProcessing.Models;

/*
FILE ORGANISATION
/g/data/py18
|-- <product>
  |-- <nature of data> or <project_id>
     |-- <MIP-era>
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
  <product> is BARPA
  <nature of data> or <project_id> is output or CORDEX for data for 
            CORDEX-CMIP6.
  <MIP-era> is the cycle of CMIP defines experiment and data specifications. 
            We use CMIP6.
  <activity_id> is DD for dynamical downscaling
  <domain_id> is spatial domain and grid resolution of the data, namely 
            AUS-15, AUST-04
  <RCM-institution> is BOM
  <driving_source_id> is the global driving model downscaled by BARPA. The 
            models selected are: 
            ERA5, ACCESS-CM2, ACCESS-ESM1-5, NorESM2-MM, EC-Earth3, CESM2, 
            CMCC-ESM2, MPI-ESM1-2-HR
  <driving_experiment_id> is evaluation (for ERA5), historical or ssp126, 
            ssp370 for CMIP6 experiments.
  <driving_variant_label> labels the ensemble member of the CMIP6 simulation 
            that produced forcing data.
  <source_id> is either BARPA-R or BARPA-C, refer to Extended Documentation
  <version_realisation> identifies the modelling version (TBC on identifying 
            data version)
  <freq> is the time frequency of the data: 1hr (1-hourly), 3hr, 6hr, 
            day (daily), mon (monthly), fx
  <variable_id> is the variable name, mostly based on,
            https://docs.google.com/spreadsheets/d/1qUauozwXkq7r1g-L4ALMIkCNINIhhCPx/edit#gid=1672965248
  <version> denotes the date of data generation or date of data release
  <netcdf filename> is 
            <variable_id>_<domain_id>_<driving_source_id>_<driving_experiment_id>_<driving_variant_label>_<RCM-institution_id>_<source_id>_<version_realisation>_<freq>[_<StartTime>-<EndTime>].nc
*/
public partial class BarpaDataset : IClimateDataset
{
    private const string product = "BARPA";
    private const string mipEra = "CMIP6";
    private const string activity = "DD";
    private const string institution = "BOM";
    private const string versionRealisation = "v1-r1";
    private const string version = "latest";

    private static readonly IReadOnlyDictionary<ClimateVariable, (string name, string units)> variableMap = new Dictionary<ClimateVariable, (string name, string units)>()
    {
        { ClimateVariable.SpecificHumidity, ("huss", "1") },
        { ClimateVariable.RelativeHumidity, ("hurs", "%") },
        { ClimateVariable.Temperature, ("tas", "K") },
        { ClimateVariable.WindSpeed, ("sfcWind", "m s-1") },
        { ClimateVariable.Precipitation, ("pr", "kg m-2 s-1") },
        { ClimateVariable.ShortwaveRadiation, ("rsds", "W m-2") },
        { ClimateVariable.SurfacePressure, ("ps", "Pa") }
    };

    private readonly BarpaProject project;
    private readonly BarpaDomain domain;
    private readonly BarpaGcm gcm;
    private readonly BarpaExperiment experiment;
    private readonly BarpaFrequency frequency;
    private readonly string basePath;

    /// <summary>
    /// Initializes a new instance of the <see cref="BarpaDataset"/> class.
    /// </summary>
    /// <param name="basePath">The base path to the BARPA dataset.</param>
    /// <param name="project">The BARPA project.</param>
    /// <param name="domain">The spatial domain and grid resolution of the data.</param>
    /// <param name="gcm">The global driving model downscaled by BARPA.</param>
    /// <param name="experiment">The experiment identifier.</param>
    /// <param name="frequency">The time frequency of the data.</param>
    public BarpaDataset(string basePath,
                        BarpaProject project,
                        BarpaDomain domain,
                        BarpaGcm gcm,
                        BarpaExperiment experiment,
                        BarpaFrequency frequency)
    {
        this.basePath = basePath;
        this.project = project;
        this.domain = domain;
        this.gcm = gcm;
        this.experiment = experiment;
        this.frequency = frequency;
    }

    /// <inheritdoc />
    public string DatasetName => $"{domain.ToDomainId()}_{gcm.ToGcmId()}_{experiment.ToExperimentId()}_{gcm.GetVariantLabel()}_{institution}_{domain.GetSource(project)}_{versionRealisation}_{frequency.ToFrequencyId()}";

    /// <inheritdoc />
    public string GenerateOutputFileName(ClimateVariable variable, VariableInfo metadata)
    {
        string variableName = metadata.Name;
        string domainName = domain.ToDomainId();
        string gcmName = gcm.ToGcmId();
        string experimentName = experiment.ToExperimentId();
        string variantLabel = gcm.GetVariantLabel();
        string sourceId = domain.GetSource(project);
        string frequencyId = frequency.ToFrequencyId();

        List<string> inputFiles = GetInputFiles(variable).ToList();
        if (inputFiles.Count == 0)
            throw new InvalidOperationException($"No input files found for variable {variable}.");

        DateTime startDate = GetDateFromFileName(inputFiles.First(), start: true);
        DateTime endDate = GetDateFromFileName(inputFiles.Last(), start: false);

        string startName = startDate.ToString("yyyyMMddHHmm");
        string endName = endDate.ToString("yyyyMMddHHmm");
        return $"{variableName}_{domainName}_{gcmName}_{experimentName}_{variantLabel}_{institution}_{sourceId}_{versionRealisation}_{frequencyId}_{startName}-{endName}.nc";
    }

    /// <inheritdoc />
    public IEnumerable<string> GetInputFiles(ClimateVariable variable)
    {
        string directory = GetInputFilesDirectory(variable);
        if (!Directory.Exists(directory))
            throw new DirectoryNotFoundException($"Input files directory {directory} does not exist.");

        return Directory.GetFiles(directory, "*.nc")
                        .OrderBy(f => GetDateFromFileName(f, start: true))
                        .ToList();
    }

    /// <inheritdoc />
    public string GetInputFilesDirectory(ClimateVariable variable)
    {
        string baseDir = Path.Combine(
            basePath,
            product,
            project.ToProjectId());

        // .../CMIP6/... only for output project.
        if (project == BarpaProject.Output)
            baseDir = Path.Combine(baseDir, mipEra);

        return Path.Combine(
            baseDir,
            activity,
            domain.ToDomainId(),
            institution,
            gcm.ToGcmId(),
            experiment.ToExperimentId(),
            gcm.GetVariantLabel(),
            domain.GetSource(project),
            versionRealisation,
            frequency.ToFrequencyId(),
            GetVariableInfo(variable).Name,
            version);
    }

    /// <inheritdoc />
    public string GetOutputDirectory()
    {
        return Path.Combine(
            project.ToProjectId(),
            domain.ToDomainId(),
            gcm.ToGcmId(),
            experiment.ToExperimentId(),
            frequency.ToFrequencyId());
    }

    /// <inheritdoc />
    public IEnumerable<IVariableProcessor> GetProcessors(IJobCreationContext context)
    {
        List<ClimateVariable> standard = [
            ClimateVariable.Temperature,
            ClimateVariable.SpecificHumidity,
            ClimateVariable.Precipitation,
            ClimateVariable.SurfacePressure,
            ClimateVariable.ShortwaveRadiation,
            ClimateVariable.WindSpeed
        ];

        List<IVariableProcessor> processors = standard.Select(v => new StandardVariableProcessor(v))
                                                      .Cast<IVariableProcessor>()
                                                      .ToList();

        if (context.Config.Version == ModelVersion.Dave)
        {
            VPDMethod method = context.Config.VPDMethod;
            VpdCalculator vpdCalculator = new VpdCalculator(method);
            processors.Add(new RechunkProcessorDecorator(vpdCalculator));
        }

        // TODO: calculate min/max daily temperature from subdaily values, when
        // input timestep is subdaily and output is daily.

        return processors;
    }

    /// <inheritdoc />
    /// <exception cref=""ArgumentException">Thrown when the variable is not supported.</exception>
    public VariableInfo GetVariableInfo(ClimateVariable variable)
    {
        if (!variableMap.TryGetValue(variable, out var info))
            throw new ArgumentException($"Variable {variable} is not supported.", nameof(variable));

        return new VariableInfo(info.name, info.units);
    }

    /// <summary>
    /// Parse a filename and extract the start or end date from it.
    /// </summary>
    /// <param name="fileName">The filename to parse.</param>
    /// <param name="start">Whether to extract the start date (true) or end date (false).</param>
    /// <returns>The extracted date.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the filename does not match the expected pattern or the date cannot be parsed.</exception>
    private static DateTime GetDateFromFileName(string fileName, bool start)
    {
        // Get the basename.
        fileName = Path.GetFileName(fileName);

        Match match = FilenameDateRegex().Match(fileName);
        if (!match.Success)
            throw new InvalidOperationException($"Filename {fileName} does not match expected pattern.");

        string str = match.Groups[start ? 1 : 2].Value;
        if (!DateTime.TryParseExact(str,
                                    "yyyyMMddHHmm",
                                    null,
                                    DateTimeStyles.None,
                                    out DateTime date))
        {
            throw new InvalidOperationException($"Could not parse date {str} from filename {fileName}.");
        }

        return date;
    }

    [GeneratedRegex(@"_(\d{12})-(\d{12})\.nc$")]
    private static partial Regex FilenameDateRegex();
}

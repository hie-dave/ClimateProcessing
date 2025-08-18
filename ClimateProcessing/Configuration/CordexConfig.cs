using ClimateProcessing.Models;
using ClimateProcessing.Models.Cordex;
using CommandLine;

namespace ClimateProcessing.Configuration;

/// <summary>
/// Contains cordex-specific CLI options.
/// </summary>
[Verb("cordex", HelpText = "Process CORDEX-CMIP6 data.")]
public class CordexConfig : ProcessingConfig
{
    [Option("activity", HelpText = "Activities to process. Valid values: DD, bias-adjusted-output. Default: process all activities.")]
    public IEnumerable<string>? Activities { get; set; }

    [Option("experiment", HelpText = "Experiments to process. Valid values: historical, ssp126, ssp370. Default: process all experiments.")]
    public IEnumerable<string>? Experiments { get; set; }

    [Option("gcm", HelpText = "Global Climate Models to process. Valid values: ACCESS-CM2, ACCESS-ESM1-5, CESM2, CMCC-ESM2, EC-Earth3, MPI-ESM1-2-HR, NorESM2-MM. Default: process all GCMs.")]
    public IEnumerable<string>? GCMs { get; set; }

    [Option("institution", HelpText = "Institutions to process. Valid values: BOM, CSIRO. Default: process all institutions.")]
    public IEnumerable<string>? Institutions { get; set; }

    [Option("source", HelpText = "Sources to process. Valid values: BARPA-R, CCAM-v2203-SN. Default: process all sources.")]
    public IEnumerable<string>? Sources { get; set; }

    /// <inheritdoc />
    public override IEnumerable<IClimateDataset> CreateDatasets()
    {
        IEnumerable<CordexActivity> activities = GetActivities();
        IEnumerable<CordexExperiment> experiments = GetExperiments();
        IEnumerable<CordexGcm> gcms = GetGCMs();
        IEnumerable<CordexInstitution> institutions = GetInstitutions();
        IEnumerable<CordexSource> sources = GetSources();

        return from activity in activities
               from experiment in experiments
               from gcm in gcms
               from institution in institutions
               from source in sources
               select new CordexDataset(
                   basePath: InputDirectory,
                   activity: activity,
                   domain: CordexDomain.Aust05i, // The only valid value
                   institution: institution,
                   gcm: gcm,
                   experiment: experiment,
                   source: source);
    }

    /// <summary>
    /// Get the list of activities to process.
    /// </summary>
    /// <returns>The list of activities to process.</returns>
    private IEnumerable<CordexActivity> GetActivities()
    {
        if (Activities == null || !Activities.Any())
            return Enum.GetValues<CordexActivity>();
        return Activities.Select(CordexActivityExtensions.FromString);
    }

    /// <summary>
    /// Get the list of experiments to process.
    /// </summary>
    /// <returns>The list of experiments to process.</returns>
    private IEnumerable<CordexExperiment> GetExperiments()
    {
        if (Experiments == null || !Experiments.Any())
            return Enum.GetValues<CordexExperiment>();
        return Experiments.Select(CordexExperimentExtensions.FromString);
    }

    /// <summary>
    /// Get the list of GCMs to process.
    /// </summary>
    /// <returns>The list of GCMs to process.</returns>
    private IEnumerable<CordexGcm> GetGCMs()
    {
        if (GCMs == null || !GCMs.Any())
            return Enum.GetValues<CordexGcm>();
        return GCMs.Select(CordexGcmExtensions.FromString);
    }

    /// <summary>
    /// Get the list of institutions to process.
    /// </summary>
    /// <returns>The list of institutions to process.</returns>
    private IEnumerable<CordexInstitution> GetInstitutions()
    {
        if (Institutions == null || !Institutions.Any())
            return Enum.GetValues<CordexInstitution>();
        return Institutions.Select(CordexInstitutionExtensions.FromString);
    }

    /// <summary>
    /// Get the list of sources to process.
    /// </summary>
    /// <returns>The list of sources to process.</returns>
    private IEnumerable<CordexSource> GetSources()
    {
        if (Sources == null || !Sources.Any())
            return Enum.GetValues<CordexSource>();
        return Sources.Select(CordexSourceExtensions.FromString);
    }
}

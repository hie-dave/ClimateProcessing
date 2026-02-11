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
    [Option("cordex-version", HelpText = "Versions to process. Valid values: v1-r1, v1-r1-ACS-MRNBC-AGCDv1-1960-2022, v1-r1-ACS-MRNBC-BARRAR2-1980-2022, v1-r1-ACS-QME-AGCDv1-1960-2022, v1-r1-ACS-QME-BARRAR2-1980-2022. Default: process all versions.")]
    public IEnumerable<string>? Versions { get; set; }

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
        IEnumerable<CordexVersion> versions = GetVersions();
        IEnumerable<CordexActivity> activities = GetActivities();
        IEnumerable<CordexExperiment> experiments = GetExperiments();
        IEnumerable<CordexGcm> gcms = GetGCMs();
        IEnumerable<CordexInstitution> institutions = GetInstitutions();
        IEnumerable<CordexSource> sources = GetSources();

        return from activity in activities.Distinct()
               from version in versions.Where(v => v.IsSupportedFor(activity)).Distinct()
               from experiment in experiments.Distinct()
               from gcm in gcms.Distinct()
               from institution in institutions.Distinct()
               from source in sources.Distinct()
               select new CordexDataset(
                   basePath: InputDirectory,
                   activity: activity,
                   domain: CordexDomain.Aust05i, // The only valid value
                   institution: institution,
                   gcm: gcm,
                   experiment: experiment,
                   source: source,
                   version: version);
    }

    public override void Validate()
    {
        base.Validate();

        if (InputTimeStepHours != 24)
            throw new ArgumentException("Input timestep must be daily (24 hours) for CORDEX datasets.", nameof(InputTimeStep));

        // Output timestep >= input timestep is guaranteed by base class.

        // Guarantee that all versions and activities are supported.
        IEnumerable<CordexVersion> versions = GetVersions();
        IEnumerable<CordexActivity> activities = GetActivities();
        foreach (var version in versions)
            if (!activities.Any(a => version.IsSupportedFor(a)))
                throw new ArgumentException($"Version {version} is not supported for any of the selected activities ({string.Join(", ", activities.Select(a => a.ToString()))}).", nameof(version));

        foreach (var activity in activities)
            if (!versions.Any(v => v.IsSupportedFor(activity)))
                throw new ArgumentException($"Activity {activity} is not supported for any of the selected versions ({string.Join(", ", versions.Select(v => v.ToString()))}).", nameof(activity));

        // CSIRO + BARPA-R not supported.
        if (GetInstitutions().Contains(CordexInstitution.CSIRO) && GetSources().Contains(CordexSource.BarpaR))
            throw new ArgumentException("The combination of CSIRO and BARPA-R is not supported.");
    }

    /// <summary>
    /// Get the list of versions to process.
    /// </summary>
    /// <returns>The list of versions to process.</returns>
    internal IEnumerable<CordexVersion> GetVersions()
    {
        if (Versions == null || !Versions.Any())
            return Enum.GetValues<CordexVersion>();
        return Versions.Select(CordexVersionExtensions.FromString);
    }

    /// <summary>
    /// Get the list of activities to process.
    /// </summary>
    /// <returns>The list of activities to process.</returns>
    internal IEnumerable<CordexActivity> GetActivities()
    {
        if (Activities == null || !Activities.Any())
            return Enum.GetValues<CordexActivity>();
        return Activities.Select(CordexActivityExtensions.FromString);
    }

    /// <summary>
    /// Get the list of experiments to process.
    /// </summary>
    /// <returns>The list of experiments to process.</returns>
    internal IEnumerable<CordexExperiment> GetExperiments()
    {
        if (Experiments == null || !Experiments.Any())
            return Enum.GetValues<CordexExperiment>();
        return Experiments.Select(CordexExperimentExtensions.FromString);
    }

    /// <summary>
    /// Get the list of GCMs to process.
    /// </summary>
    /// <returns>The list of GCMs to process.</returns>
    internal IEnumerable<CordexGcm> GetGCMs()
    {
        if (GCMs == null || !GCMs.Any())
            return Enum.GetValues<CordexGcm>();
        return GCMs.Select(CordexGcmExtensions.FromString);
    }

    /// <summary>
    /// Get the list of institutions to process.
    /// </summary>
    /// <returns>The list of institutions to process.</returns>
    internal IEnumerable<CordexInstitution> GetInstitutions()
    {
        if (Institutions == null || !Institutions.Any())
            return Enum.GetValues<CordexInstitution>();
        return Institutions.Select(CordexInstitutionExtensions.FromString);
    }

    /// <summary>
    /// Get the list of sources to process.
    /// </summary>
    /// <returns>The list of sources to process.</returns>
    internal IEnumerable<CordexSource> GetSources()
    {
        if (Sources == null || !Sources.Any())
            return Enum.GetValues<CordexSource>();
        return Sources.Select(CordexSourceExtensions.FromString);
    }
}

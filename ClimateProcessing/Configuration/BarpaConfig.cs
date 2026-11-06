using ClimateProcessing.Models;
using ClimateProcessing.Models.Barpa;
using CommandLine;

namespace ClimateProcessing.Configuration;

/// <summary>
/// Configuration for processing BARPA data.
/// </summary>
[Verb("barpa", HelpText = "Process BARPA data.")]
public class BarpaConfig : ProcessingConfig
{
    [Option("gcm", HelpText = "GCMs to process. Valid values: ACCESS-CM2, ACCESS-ESM1-5, CESM2, CMCC-ESM2, EC-Earth3, MPI-ESM1-2-HR, NorESM2-MM, ERA5. Default: process all valid GCMs for the underlying config.")]
    public IEnumerable<string>? GCMs { get; set; }

    [Option("domain", HelpText = "Domains to process. Valid values: AUST-04, AUS-15, AUS-20i. Default: process all valid domains for the underlying config.")]
    public IEnumerable<string>? Domains { get; set; }

    [Option("project", HelpText = "Projects to process. Valid values: output, output-CMIP6, cordex-CMIP6. Default: process all valid projects for the underlying config.")]
    public IEnumerable<string>? Projects { get; set; }

    [Option("experiment", HelpText = "Experiments to process. Valid values: historical, ssp126, ssp370, ssp585. Default: process all valid experiments for the underlying config.")]
    public IEnumerable<string>? Experiments { get; set; }

    public override IEnumerable<IClimateDataset> CreateDatasets()
    {
        IEnumerable<BarpaGcm> gcms = GetGcms();
        IEnumerable<BarpaDomain> domains = GetDomains();
        IEnumerable<BarpaProject> projects = GetProjects();
        IEnumerable<BarpaExperiment> experiments = GetExperiments();
        BarpaFrequency frequency = GetFrequency();

        List<IClimateDataset> datasets = new List<IClimateDataset>();

        // If the user doesn't specify a dimension (e.g. gcm, domain, etc), then
        // we want to run for all valid values on that dimension. To do this, we
        // iterate through all valid values on that dimension, and skip any
        // combinations with the other dimensions which don't produce a valid
        // dataset.
        foreach (BarpaProject project in projects)
        {
            if (!frequency.IsValidFor(project))
                continue;

            foreach (BarpaDomain domain in domains)
            {
                if (!domain.IsValidFor(project))
                    continue;

                foreach (BarpaGcm gcm in gcms)
                {
                    if (!gcm.IsValidFor(project, domain))
                        continue;

                    foreach (BarpaExperiment experiment in experiments)
                    {
                        if (!experiment.IsValidFor(gcm, project, domain))
                            continue;

                        datasets.Add(new BarpaDataset(
                            InputDirectory,
                            project,
                            domain,
                            gcm,
                            experiment,
                            frequency));
                    }
                }
            }
        }

        return datasets;
    }

    public override void Validate()
    {
        base.Validate();

        IEnumerable<BarpaGcm> gcms = LiteralGcms();
        IEnumerable<BarpaDomain> domains = LiteralDomains();
        IEnumerable<BarpaProject> projects = LiteralProjects();
        IEnumerable<BarpaExperiment> experiments = LiteralExperiments();
        BarpaFrequency frequency = GetFrequency();

        foreach (BarpaProject project in projects)
        {
            if (!frequency.IsValidFor(project))
                throw new ArgumentException($"Frequency {frequency} is not valid for project {project}.");

            foreach (BarpaDomain domain in domains)
            {
                if (!domain.IsValidFor(project))
                    throw new ArgumentException($"Domain {domain} is not valid for project {project}.");

                foreach (BarpaGcm gcm in gcms)
                {
                    if (!gcm.IsValidFor(project, domain))
                        throw new ArgumentException($"GCM {gcm} is not valid for project {project} and domain {domain}.");

                    foreach (BarpaExperiment experiment in experiments)
                    {
                        if (!experiment.IsValidFor(gcm, project, domain))
                            throw new ArgumentException($"Experiment {experiment} is not valid for GCM {gcm}, project {project}, and domain {domain}.");
                    }
                }
            }
        }
    }

    internal IEnumerable<BarpaGcm> GetGcms()
    {
        if (GCMs == null || !GCMs.Any())
            return Enum.GetValues<BarpaGcm>();
        return LiteralGcms();
    }

    internal IEnumerable<BarpaDomain> GetDomains()
    {
        if (Domains == null || !Domains.Any())
            return Enum.GetValues<BarpaDomain>();
        return LiteralDomains();
    }

    internal IEnumerable<BarpaProject> GetProjects()
    {
        if (Projects == null || !Projects.Any())
            return Enum.GetValues<BarpaProject>();
        return LiteralProjects();
    }

    internal IEnumerable<BarpaExperiment> GetExperiments()
    {
        if (Experiments == null || !Experiments.Any())
            return Enum.GetValues<BarpaExperiment>();
        return LiteralExperiments();
    }

    private BarpaFrequency GetFrequency()
    {
        return InputTimeStep.Hours switch
        {
            1 => BarpaFrequency.Hourly,
            3 => BarpaFrequency.ThreeHourly,
            6 => BarpaFrequency.SixHourly,
            24 => BarpaFrequency.Daily,
            _ => throw new ArgumentException($"Invalid input time step: {InputTimeStep.Hours}")
        };
    }

    private IEnumerable<BarpaGcm> LiteralGcms()
        => GCMs?.Select(BarpaGcmExtensions.FromString) ?? Array.Empty<BarpaGcm>();

    private IEnumerable<BarpaDomain> LiteralDomains()
        => Domains?.Select(BarpaDomainExtensions.FromString) ?? Array.Empty<BarpaDomain>();

    private IEnumerable<BarpaProject> LiteralProjects()
        => Projects?.Select(BarpaProjectExtensions.FromString) ?? Array.Empty<BarpaProject>();

    private IEnumerable<BarpaExperiment> LiteralExperiments()
        => Experiments?.Select(BarpaExperimentExtensions.FromString) ?? Array.Empty<BarpaExperiment>();
}

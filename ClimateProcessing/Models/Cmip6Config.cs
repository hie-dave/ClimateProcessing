using ClimateProcessing.Models.Cmip6;
using CommandLine;

namespace ClimateProcessing.Models;

/// <summary>
/// Contains CMIP6-specific CLI options.
/// </summary>
[Verb("cmip6", HelpText = "Process CMIP6 data.")]
public class Cmip6Config : ProcessingConfig
{
    [Option("gcm", HelpText = "GCMs to process. Default: process all GCMs.")]
    public IEnumerable<string>? Gcms { get; set; }

    [Option("experiment", HelpText = "Experiments to process. Default: process all experiments.")]
    public IEnumerable<string>? Experiments { get; set; }

    /// <inheritdoc />
    public Cmip6Config()
    {
        // Default to hourly.
        InputTimeStepHours = 1;
    }

    /// <inheritdoc /> 
    public override IEnumerable<Cmip6Dataset> CreateDatasets()
    {
        IEnumerable<Cmip6Gcm> gcms = GetGcms();
        IEnumerable<Cmip6Experiment> experiments = GetExperiments();

        return from gcm in gcms
               from experiment in experiments
               select new Cmip6Dataset(
                   InputDirectory,
                   gcm,
                   experiment);
    }

    /// <summary>
    /// Get the list of GCMs to process.
    /// </summary>
    /// <returns>The list of GCMs to process.</returns>
    private IEnumerable<Cmip6Gcm> GetGcms()
    {
        if (Gcms == null || !Gcms.Any())
            return Enum.GetValues<Cmip6Gcm>();
        return Gcms.Select(Cmip6GcmExtensions.FromString);
    }

    /// <summary>
    /// Get the list of experiments to process.
    /// </summary>
    /// <returns>The list of experiments to process.</returns>
    private IEnumerable<Cmip6Experiment> GetExperiments()
    {
        if (Experiments == null || !Experiments.Any())
            return Enum.GetValues<Cmip6Experiment>();
        return Experiments.Select(Cmip6ExperimentExtensions.FromString);
    }
}

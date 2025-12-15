using ClimateProcessing.Models.Barra2;
using CommandLine;

namespace ClimateProcessing.Models;

/// <summary>
/// Contains narclim2-specific CLI options.
/// </summary>
[Verb("barra2", HelpText = "Process NARCliM2 data.")]
public class Barra2Config : ProcessingConfig
{
    [Option("domain", HelpText = "Domains to process. Valid values: AUS-11, AUS-22, AUST-04, AUST-11. Default: process all domains.")]
    public IEnumerable<string>? Domains { get; set; }

    [Option("frequency", HelpText = "Frequencies to process. Valid values: 1hr, 3hr, 6hr, daily, monthly. Default: process 1-hourly.")]
    public IEnumerable<string>? Frequencies { get; set; }

    [Option("grid", HelpText = "Grids to process. Valid values: R2, RE2, C2. Default: process all grids.")]
    public IEnumerable<string>? Grids { get; set; }

    [Option("variant", HelpText = "Variants to process. Valid values: hres, eda. Default: process hres.")]
    public IEnumerable<string>? Variants { get; set; }

    /// <inheritdoc /> 
    public override IEnumerable<Barra2Dataset> CreateDatasets()
    {
        IEnumerable<Barra2Domain> domains = GetDomains();
        IEnumerable<Barra2Frequency> frequencies = GetFrequencies();
        IEnumerable<Barra2Grid> grids = GetGrids();
        IEnumerable<Barra2Variant> variants = GetVariants();

        return from domain in domains
               from frequency in frequencies
               from grid in grids
               from variant in variants
               select new Barra2Dataset(
                   InputDirectory,
                   domain,
                   frequency,
                   grid,
                   variant);
    }

    /// <summary>
    /// Get the list of domains to process.
    /// </summary>
    /// <returns>The list of domains to process.</returns>
    private IEnumerable<Barra2Domain> GetDomains()
    {
        if (Domains == null || !Domains.Any())
            return Enum.GetValues<Barra2Domain>();
        return Domains.Select(Barra2DomainExtensions.FromString);
    }

    /// <summary>
    /// Get the list of frequencies to process.
    /// </summary>
    /// <returns>The list of frequencies to process.</returns>
    private IEnumerable<Barra2Frequency> GetFrequencies()
    {
        if (Frequencies == null || !Frequencies.Any())
            // Default to 1-hourly
            return [Barra2Frequency.Hour1];
        return Frequencies.Select(Barra2FrequencyExtensions.FromString);
    }

    /// <summary>
    /// Get the list of grids to process.
    /// </summary>
    /// <returns>The list of grids to process.</returns>
    private IEnumerable<Barra2Grid> GetGrids()
    {
        if (Grids == null || !Grids.Any())
            return Enum.GetValues<Barra2Grid>();
        return Grids.Select(Barra2GridExtensions.FromString);
    }

    /// <summary>
    /// Get the list of variants to process.
    /// </summary>
    /// <returns>The list of variants to process.</returns>
    private IEnumerable<Barra2Variant> GetVariants()
    {
        if (Variants == null || !Variants.Any())
            return Enum.GetValues<Barra2Variant>();
        return Variants.Select(Barra2VariantExtensions.FromString);
    }
}

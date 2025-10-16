using ClimateProcessing.Models;
using CommandLine;

namespace ClimateProcessing.Configuration;

/// <summary>
/// Contains silo-specific CLI options.
/// </summary>
[Verb("silo", HelpText = "Process SILO data.")]
public class SiloConfig : ProcessingConfig
{
    // Silo has no dataset-specific options at present. Therefore, no custom
    // validation logic is required.

    /// <inheritdoc />
    public override IEnumerable<IClimateDataset> CreateDatasets()
    {
        return [new SiloDataset(InputDirectory)];
    }
}

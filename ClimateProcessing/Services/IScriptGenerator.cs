using ClimateProcessing.Models;

namespace ClimateProcessing.Services;

/// <summary>
/// Interface for dataset-specific script generators.
/// </summary>
public interface IScriptGenerator
{
    /// <summary>
    /// Generate processing scripts for the dataset.
    /// </summary>
    /// <param name="dataset">The dataset to process.</param>
    /// <returns>The path to the top-level script.</returns>
    Task<string> GenerateScriptsAsync(IClimateDataset dataset);
}

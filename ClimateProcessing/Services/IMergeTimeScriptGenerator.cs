using ClimateProcessing.Models.Options;

namespace ClimateProcessing.Services;

/// <summary>
/// Interface to a class which generates a mergetime script.
/// </summary>
public interface IMergetimeScriptGenerator
{
    /// <summary>
    /// Writes a script that merges time-series data files.
    /// </summary>
    /// <param name="writer">The script writer to write to.</param>
    /// <param name="options">Options for the mergetime operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task WriteMergetimeScriptAsync(IFileWriter writer, IMergetimeOptions options);
}

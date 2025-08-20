using ClimateProcessing.Models.Options;

namespace ClimateProcessing.Services;

/// <summary>
/// Interface to a class which generates a rechunk script.
/// </summary>
public interface IRechunkScriptGenerator
{
    /// <summary>
    /// Writes a script that rechunks a dataset.
    /// </summary>
    /// <param name="writer">The script writer to write to.</param>
    /// <param name="options">Options for the rechunk operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task WriteRechunkScriptAsync(IFileWriter writer, IRechunkOptions options);
}

using ClimateProcessing.Models.Options;

namespace ClimateProcessing.Services;

/// <summary>
/// Interface to a class which generates a preprocessing script.
/// </summary>
public interface IPreprocessingScriptGenerator
{
    /// <summary>
    /// Writes a script that preprocesses a variable.
    /// </summary>
    /// <param name="writer">The script writer to write to.</param>
    /// <param name="options">Options for the preprocessing operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task WritePreprocessingScriptAsync(IFileWriter writer, IPreprocessingOptions options);
}

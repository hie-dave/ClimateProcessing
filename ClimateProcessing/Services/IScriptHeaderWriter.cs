using ClimateProcessing.Models;

namespace ClimateProcessing.Services;

/// <summary>
/// Handles generation of PBS-specific script content and headers.
/// </summary>
public interface IScriptHeaderWriter
{
    /// <summary>
    /// Write the script header.
    /// </summary>
    /// <param name="writer">The file writer to which the header will be written.</param>
    /// <param name="jobName">The job name.</param>
    /// <param name="storageDirectives">The storage directives required by the job.</param>
    /// <remarks>
    /// TODO: refactor the PBS-specific argument out of this interface. It
    /// doesn't belong here.
    /// </remarks>
    Task WriteHeaderAsync(
        IFileWriter writer,
        string jobName,
        IEnumerable<PBSStorageDirective> storageDirectives);
}

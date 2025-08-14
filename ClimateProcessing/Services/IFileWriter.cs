namespace ClimateProcessing.Services;

/// <summary>
/// An interface to a class which writes script content.
/// </summary>
public interface IFileWriter : IDisposable
{
    /// <summary>
    /// The path to the file being written to.
    /// </summary>
    string FilePath { get; }

    /// <summary>
    /// Write the specified content to the file.
    /// </summary>
    /// <param name="content">The content to write.</param>
    Task WriteAsync(string content);

    /// <summary>
    /// Write the specified line to the file, followed by a newline.
    /// </summary>
    /// <param name="line">The line to write.</param>
    Task WriteLineAsync(string line);

    /// <summary>
    /// Write an empty line to the file.
    /// </summary>
    Task WriteLineAsync();
}

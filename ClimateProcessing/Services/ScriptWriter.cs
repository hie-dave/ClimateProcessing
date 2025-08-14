using System.Runtime.Versioning;

namespace ClimateProcessing.Services;

/// <summary>
/// An implementation of <see cref="IFileWriter"/> which writes to a file.
/// </summary>
public class ScriptWriter : IFileWriter
{
    /// <summary>
    /// The text writer.
    /// </summary>
    private readonly TextWriter writer;

    /// <summary>
    /// The file being written to.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Creates a new script writer.
    /// </summary>
    /// <param name="file">The path to the file.</param>
    public ScriptWriter(string file)
    {
        FilePath = file;
        writer = File.CreateText(file);
#if !WINDOWS
#pragma warning disable CA1416
        InitialiseScript();
#pragma warning restore CA1416
#endif
    }

    /// <inheritdoc/>
    public Task WriteAsync(string content)
    {
        return writer.WriteAsync(content);
    }

    /// <inheritdoc/>
    public Task WriteLineAsync(string line)
    {
        return writer.WriteLineAsync(line);
    }

    /// <inheritdoc/>
    public Task WriteLineAsync()
    {
        return writer.WriteLineAsync();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        writer.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Create the specified script file, and give it the required permissions.
    /// </summary>
    [UnsupportedOSPlatform("windows")]
    private void InitialiseScript()
    {
        // Set permissions to 755.
        File.SetUnixFileMode(FilePath,
            UnixFileMode.UserRead |
            UnixFileMode.UserWrite |
            UnixFileMode.UserExecute |
            UnixFileMode.GroupRead |
            UnixFileMode.GroupExecute |
            UnixFileMode.OtherRead |
            UnixFileMode.OtherExecute);
    }
}

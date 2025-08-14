namespace ClimateProcessing.Services;

/// <summary>
/// A factory for creating <see cref="IFileWriter"/> instances.
/// </summary>
public interface IFileWriterFactory
{
    /// <summary>
    /// Creates a new <see cref="IFileWriter"/> instance.
    /// </summary>
    /// <param name="file">The path to the file.</param>
    /// <returns>The created <see cref="IFileWriter"/> instance.</returns>
    IFileWriter Create(string file);
}

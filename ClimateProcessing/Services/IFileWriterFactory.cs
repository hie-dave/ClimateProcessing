namespace ClimateProcessing.Services;

/// <summary>
/// A factory for creating <see cref="IFileWriter"/> instances.
/// </summary>
public interface IFileWriterFactory
{
    /// <summary>
    /// Creates a new <see cref="IFileWriter"/> instance.
    /// </summary>
    /// <param name="name">The script name.</param>
    /// <returns>The created <see cref="IFileWriter"/> instance.</returns>
    IFileWriter Create(string name);
}

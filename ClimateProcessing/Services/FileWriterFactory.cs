namespace ClimateProcessing.Services;

/// <summary>
/// A factory for creating <see cref="IFileWriter"/> instances.
/// </summary>
public class FileWriterFactory : IFileWriterFactory
{
    /// <inheritdoc/>
    public IFileWriter Create(string file)
    {
        return new ScriptWriter(file);
    }
}

using ClimateProcessing.Models;

namespace ClimateProcessing.Services;

/// <summary>
/// A factory for creating <see cref="IFileWriter"/> instances.
/// </summary>
public class FileWriterFactory : IFileWriterFactory
{
    /// <summary>
    /// The path manager service.
    /// </summary>
    private readonly IPathManager pathManager;

    /// <summary>
    /// Creates a new instance of the <see cref="FileWriterFactory"/> class.
    /// </summary>
    /// <param name="pathManager">The path manager.</param>
    public FileWriterFactory(IPathManager pathManager)
    {
        this.pathManager = pathManager;
    }

    /// <inheritdoc/>
    public IFileWriter Create(string name)
    {
        string directory = pathManager.GetBasePath(PathType.Script);
        string script = Path.Combine(directory, name);
        return new ScriptWriter(script);
    }
}

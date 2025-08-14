using ClimateProcessing.Services;

namespace ClimateProcessing.Tests.Helpers;

/// <summary>
/// A factory that creates <see cref="InMemoryScriptWriter"/> instances for unit testing.
/// </summary>
public class InMemoryScriptWriterFactory : IFileWriterFactory
{
    private readonly Dictionary<string, InMemoryScriptWriter> writers = new();

    /// <summary>
    /// Creates a new <see cref="IFileWriter"/> instance that writes to memory.
    /// </summary>
    /// <param name="name">The script name.</param>
    /// <returns>The created <see cref="IFileWriter"/> instance.</returns>
    public IFileWriter Create(string name)
    {
        var writer = new InMemoryScriptWriter();
        writers[name] = writer;
        return writer;
    }

    /// <summary>
    /// Gets the content of a script that was created by this factory.
    /// </summary>
    /// <param name="name">The script name.</param>
    /// <returns>The script content, or null if the script doesn't exist.</returns>
    public string Read(string name)
    {
        if (!writers.TryGetValue(name, out var writer))
            throw new KeyNotFoundException($"Script '{name}' not found.");
        return writer.GetContent();
    }
}

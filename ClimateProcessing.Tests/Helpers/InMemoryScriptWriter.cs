using ClimateProcessing.Services;

namespace ClimateProcessing.Tests.Helpers;

public sealed class InMemoryScriptWriter : IFileWriter
{
    public const string ScriptName = "In-Memory";

    /// <summary>
    /// The string writer used to store the script content.
    /// </summary>
    private readonly StringWriter writer;

    /// <summary>
    /// ID of the script (not an actual file path).
    /// </summary>
    public string FilePath => ScriptName;

    public InMemoryScriptWriter()
    {
        writer = new StringWriter();
    }

    public string GetContent() => writer.ToString();

    public void Dispose()
    {
        writer.Dispose();
    }

    public async Task WriteAsync(string content)
    {
        await writer.WriteAsync(content);
    }

    public async Task WriteLineAsync(string line)
    {
        await writer.WriteLineAsync(line);
    }

    public async Task WriteLineAsync()
    {
        await writer.WriteLineAsync();
    }
}

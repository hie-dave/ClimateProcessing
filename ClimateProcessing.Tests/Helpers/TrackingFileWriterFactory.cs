using ClimateProcessing.Services;

namespace ClimateProcessing.Tests.Helpers;

public class TrackingFileWriterFactory : IFileWriterFactory
{
    private class DisposableWrapper : IFileWriter
    {
        private readonly IFileWriter inner;
        private readonly Action onDispose;

        public DisposableWrapper(IFileWriter inner, Action onDispose)
        {
            this.inner = inner;
            this.onDispose = onDispose;
        }

        public string FilePath => inner.FilePath;

        public Task WriteAsync(string content) => inner.WriteAsync(content);

        public Task WriteLineAsync(string line) => inner.WriteLineAsync(line);

        public Task WriteLineAsync() => inner.WriteLineAsync();

        public void Dispose()
        {
            try
            {
                inner.Dispose();
            }
            finally
            {
                onDispose();
            }
        }
    }

    private readonly HashSet<string> activeWriters = new();
    private readonly string outputDirectory;

    public IReadOnlyCollection<string> ActiveWriters => activeWriters;

    public TrackingFileWriterFactory(string outputDirectory)
    {
        this.outputDirectory = outputDirectory;
    }

    public IFileWriter Create(string name)
    {
        string path = Path.Combine(outputDirectory, name);
        activeWriters.Add(path);

        ScriptWriter writer = new(path);
        return new DisposableWrapper(writer, () => activeWriters.Remove(path));
    }
}

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

    public IReadOnlyCollection<string> ActiveWriters => activeWriters;

    public IFileWriter Create(string filePath)
    {
        activeWriters.Add(filePath);

        ScriptWriter writer = new(filePath);
        return new DisposableWrapper(writer, () => activeWriters.Remove(filePath));
    }
}

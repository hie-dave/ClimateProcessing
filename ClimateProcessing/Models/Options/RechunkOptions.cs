using ClimateProcessing.Services;

namespace ClimateProcessing.Models.Options;

/// <summary>
/// Options describing how a dataset should be rechunked.
/// </summary>
public class RechunkOptions : IRechunkOptions
{
    /// <summary>
    /// The path to the input file.
    /// </summary>
    public string InputFile { get; private init; }

    /// <summary>
    /// The path to the output file.
    /// </summary>
    public string OutputFile { get; private init; }

    /// <summary>
    /// The chunk size to use for the spatial dimensions.
    /// </summary>
    public int SpatialChunkSize { get; private init; }

    /// <summary>
    /// The chunk size to use for the time dimension.
    /// </summary>
    public int TimeChunkSize { get; private init; }

    /// <summary>
    /// The compression level to use (0-9, 0 = no compression).
    /// </summary>
    public int CompressionLevel { get; private init; }

    /// <summary>
    /// Whether to delete the input file after rechunking.
    /// </summary>
    public bool Cleanup { get; private init; }

    /// <summary>
    /// The path manager to use for file paths.
    /// </summary>
    public IPathManager PathManager { get; private init; }

    /// <summary>
    /// Creates a new rechunk options instance.
    /// </summary>
    /// <param name="inputFile">The path to the input file.</param>
    /// <param name="outputFile">The path to the output file.</param>
    /// <param name="spatialChunkSize">The chunk size to use for the spatial dimensions.</param>
    /// <param name="timeChunkSize">The chunk size to use for the time dimension.</param>
    /// <param name="compressionLevel">The compression level to use (0-9, 0 = no compression).</param>
    /// <param name="cleanup">Whether to delete the input file after rechunking.</param>
    /// <param name="pathManager">The path manager to use for file paths.</param>
    public RechunkOptions(
        string inputFile,
        string outputFile,
        int spatialChunkSize,
        int timeChunkSize,
        int compressionLevel,
        bool cleanup,
        IPathManager pathManager)
    {
        InputFile = inputFile;
        OutputFile = outputFile;
        SpatialChunkSize = spatialChunkSize;
        TimeChunkSize = timeChunkSize;
        CompressionLevel = compressionLevel;
        Cleanup = cleanup;
        PathManager = pathManager;
    }
}

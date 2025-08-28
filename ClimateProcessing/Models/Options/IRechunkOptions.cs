using ClimateProcessing.Services;

namespace ClimateProcessing.Models.Options;

/// <summary>
/// Options describing how a dataset should be rechunked.
/// </summary>
public interface IRechunkOptions
{
    /// <summary>
    /// The path to the input file.
    /// </summary>
    string InputFile { get; }

    /// <summary>
    /// The path to the output file.
    /// </summary>
    string OutputFile { get; }

    /// <summary>
    /// The chunk size to use for the spatial dimensions.
    /// </summary>
    int SpatialChunkSize { get; }

    /// <summary>
    /// The chunk size to use for the time dimension.
    /// </summary>
    int TimeChunkSize { get; }

    /// <summary>
    /// The compression level to use (0-9, 0 = no compression).
    /// </summary>
    int CompressionLevel { get; }

    /// <summary>
    /// Whether to delete the input file after rechunking.
    /// </summary>
    bool Cleanup { get; }

    /// <summary>
    /// The name of the variable to be rechunked.
    /// </summary>
    string VariableName { get; }

    /// <summary>
    /// Metadata about the variable to be rechunked.
    /// </summary>
    VariableMetadata Metadata { get; }

    /// <summary>
    /// The path manager to use for file paths.
    /// </summary>
    IPathManager PathManager { get; }
}

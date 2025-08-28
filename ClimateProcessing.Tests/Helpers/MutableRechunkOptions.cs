using ClimateProcessing.Models;
using ClimateProcessing.Models.Options;
using ClimateProcessing.Services;
using ClimateProcessing.Tests.Mocks;

namespace ClimateProcessing.Tests.Helpers;

/// <summary>
/// Options describing how a dataset should be rechunked.
/// </summary>
public class MutableRechunkOptions : IRechunkOptions
{
    /// <summary>
    /// The path to the input file.
    /// </summary>
    public string InputFile { get; set; } = "in.nc";

    /// <summary>
    /// The path to the output file.
    /// </summary>
    public string OutputFile { get; set; } = "out.nc";

    /// <summary>
    /// The chunk size to use for the spatial dimensions.
    /// </summary>
    public int SpatialChunkSize { get; set; } = 1;

    /// <summary>
    /// The chunk size to use for the time dimension.
    /// </summary>
    public int TimeChunkSize { get; set; } = 1;

    /// <summary>
    /// The compression level to use (0-9, 0 = no compression).
    /// </summary>
    public int CompressionLevel { get; set; } = 0;

    /// <summary>
    /// Whether to delete the input file after rechunking.
    /// </summary>
    public bool Cleanup { get; set; } = true;

    /// <summary>
    /// The variable name to use.
    /// </summary>
    public string VariableName { get; set; } = "var";

    /// <summary>
    /// The metadata to use.
    /// </summary>
    public VariableMetadata Metadata { get; set; } = new VariableMetadata("standard", "long");

    /// <summary>
    /// The path manager to use for file paths.
    /// </summary>
    public IPathManager PathManager { get; set; } = new MockPathManager();
}

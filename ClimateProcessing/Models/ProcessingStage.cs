namespace ClimateProcessing.Models;

/// <summary>
/// The stage of processing that a dataset or variable is at.
/// </summary>
public enum ProcessingStage
{
    /// <summary>
    /// Represents a file that has been preprocessed. E.g. unpacked, renamed,
    /// regridded, units converted, temporally aggregated, etc.
    /// </summary>
    Preprocessed,

    /// <summary>
    /// Represents a file containing the entire timeseries of data, but with
    /// time as the first dimension.
    /// </summary>
    Timeseries,

    /// <summary>
    /// Represents the file, rechunked for optimal efficiency for the model's
    /// access patterns.
    /// </summary>
    Rechunked
}

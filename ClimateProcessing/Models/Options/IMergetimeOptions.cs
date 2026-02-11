namespace ClimateProcessing.Models.Options;

public interface IMergetimeOptions
{
    /// <summary>
    /// The directory containing the input files.
    /// </summary>
    string InputDirectory { get; }

    /// <summary>
    /// The path to the output file.
    /// </summary>
    string OutputFile { get; }
}

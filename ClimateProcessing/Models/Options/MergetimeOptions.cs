namespace ClimateProcessing.Models.Options;

/// <summary>
/// Options describing how files should be merged together along their time
/// axes, as well as any other transformations that should be applied.
/// </summary>
public class MergetimeOptions : IMergetimeOptions
{
    /// <summary>
    /// The directory containing the input files.
    /// </summary>
    public string InputDirectory { get; private init; }

    /// <summary>
    /// The path to the output file.
    /// </summary>
    public string OutputFile { get; private init; }

    /// <summary>
    /// Creates a new instance of <see cref="MergetimeOptions"/>.
    /// </summary>
    /// <param name="inputDirectory">The directory containing the input files.</param>
    /// <param name="outputFile">The path to the output file.</param>
    public MergetimeOptions(string inputDirectory, string outputFile)
    {
        InputDirectory = inputDirectory;
        OutputFile = outputFile;
    }
}

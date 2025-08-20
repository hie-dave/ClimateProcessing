using ClimateProcessing.Models;
using ClimateProcessing.Models.Options;

namespace ClimateProcessing.Services;

/// <summary>
/// Default implementation of rechunk script generator that rechunks files with
/// nco.
/// </summary>
public class NcoRechunkScriptGenerator : IRechunkScriptGenerator
{
    /// <summary>
    /// Creates a new rechunk script generator.
    /// </summary>
    public NcoRechunkScriptGenerator() { }

    /// <summary>
    /// Writes a script that rechunks a dataset.
    /// </summary>
    /// <param name="writer">The script writer to write to.</param>
    /// <param name="options">Options for the rechunk operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task WriteRechunkScriptAsync(IFileWriter writer, IRechunkOptions options)
    {
        // File paths.
        // The output of the mergetime script is the input file for this script.
        await writer.WriteLineAsync($"IN_FILE=\"{options.InputFile}\"");
        await writer.WriteLineAsync($"OUT_FILE=\"{options.OutputFile}\"");
        // Reorder dimensions, improve chunking, and enable compression.

        // Note: we could use lon,lat,time but ncview works better if the
        // x-dimension precedes the y-dimension.
        string ordering = "-a lat,lon,time";
        string chunking = $"--cnk_dmn lat,{options.SpatialChunkSize} --cnk_dmn lon,{options.SpatialChunkSize} --cnk_dmn time,{options.TimeChunkSize}";

        // TODO: check if compression level <= 9?
        string compression = options.CompressionLevel > 0 ? $"-L{options.CompressionLevel}" : "";

        await writer.WriteLineAsync("log \"Rechunking files...\"");
        await writer.WriteLineAsync($"ncpdq -O {ordering} {chunking} {compression} \"${{IN_FILE}}\" \"${{OUT_FILE}}\"");
        await writer.WriteLineAsync("log \"All files rechunked successfully.\"");
        await writer.WriteLineAsync();

        // Calculate checksum.
        // Note: we change directory and use a relative file path, to ensure
        // that the checksum file remains portable.

        string outputPath = options.PathManager.GetBasePath(PathType.Output);
        string checksumFile = options.PathManager.GetChecksumFilePath();
        string relativePath = Path.GetRelativePath(outputPath, options.OutputFile);

        await writer.WriteLineAsync("# Calculate checksum.");
        await writer.WriteLineAsync($"log \"Calculating checksum...\"");
        await writer.WriteLineAsync($"cd \"{outputPath}\"");
        await writer.WriteLineAsync($"REL_PATH=\"{relativePath}\"");
        await writer.WriteLineAsync($"sha512sum \"${{REL_PATH}}\" >>\"{checksumFile}\"");
        await writer.WriteLineAsync("log \"Checksum calculation completed successfully.\"");
        await writer.WriteLineAsync();

        // We can now delete the temporary input file, but only if it's not also
        // required for the VPD estimation, which may not have occurred yet.
        if (options.Cleanup)
        {
            await writer.WriteLineAsync("# Delete temporary file.");
            await writer.WriteLineAsync($"rm -f \"${{IN_FILE}}\"");
            await writer.WriteLineAsync();
        }
        else
            await writer.WriteLineAsync("# Input file cannot (necessarily) be deleted yet, since it is required for VPD estimation.");
    }
}

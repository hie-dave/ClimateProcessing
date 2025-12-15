namespace ClimateProcessing.Models.Barra2;

/// <summary>
/// The source of the BARRA2 data. Refer to extended documentation.
/// </summary>
public enum Barra2Grid
{
    /// <summary>
    /// Deterministic reanalysis on a horizontal grid spacing of 0.11°.
    /// </summary>
    R2,

    /// <summary>
    /// Ensemble on a horizontal grid spacing of 0.22°.
    /// </summary>
    RE2,

    /// <summary>
    /// Convective-scale downscaling system based on a horizontal grid spacing
    /// of 0.04°.
    /// </summary>
    C2
}

/// <summary>
/// Extension methods for the <see cref="Barra2Grid"/> enum.
/// </summary>
public static class Barra2GridExtensions
{
    /// <summary>
    /// The string representation of the <see cref="Barra2Grid.R2"/> source.
    /// </summary>
    private const string r2 = "BARRA-R2";

    /// <summary>
    /// The string representation of the <see cref="Barra2Grid.RE2"/> source.
    /// </summary>
    private const string re2 = "BARRA-RE2";

    /// <summary>
    /// The string representation of the <see cref="Barra2Grid.C2"/> source.
    /// </summary>
    private const string c2 = "BARRA-C2";

    /// <summary>
    /// Convert a <see cref="Barra2Grid"/> to a string.
    /// </summary>
    /// <param name="source">The source to convert.</param>
    /// <returns>The string representation of the source.</returns>
    /// <exception cref="ArgumentException">Thrown when the source is not a valid <see cref="Barra2Grid"/>.</exception>
    public static string ToString(this Barra2Grid source) => source switch
    {
        Barra2Grid.R2 => r2,
        Barra2Grid.RE2 => re2,
        Barra2Grid.C2 => c2,
        _ => throw new ArgumentException($"Unknown source: {source}")
    };

    /// <summary>
    /// Convert a string to a <see cref="Barra2Grid"/>.
    /// </summary>
    /// <param name="sourceStr">The string to convert.</param>
    /// <returns>The <see cref="Barra2Grid"/> representation of the string.</returns>
    /// <exception cref="ArgumentException">Thrown when the string is not a valid <see cref="Barra2Grid"/>.</exception>
    public static Barra2Grid FromString(string sourceStr) => sourceStr switch
    {
        r2 => Barra2Grid.R2,
        re2 => Barra2Grid.RE2,
        c2 => Barra2Grid.C2,
        _ => throw new ArgumentException($"Unknown source string: {sourceStr}")
    };
}

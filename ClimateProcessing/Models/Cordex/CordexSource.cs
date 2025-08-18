namespace ClimateProcessing.Models.Cordex;

/// <summary>
/// The source of the CORDEX dataset.
/// </summary>
public enum CordexSource
{
    /// <summary>
    /// BARPA-R
    /// </summary>
    BarpaR,

    /// <summary>
    /// CCAM-v2203-SN
    /// </summary>
    Ccamv2203SN
}

/// <summary>
/// Extension methods for the <see cref="CordexSource"/> enum.
/// </summary>
public static class CordexSourceExtensions
{
    private const string barpaR = "BARPA-R";
    private const string ccamv2203SN = "CCAM-v2203-SN";

    /// <summary>
    /// Converts the <see cref="CordexSource"/> to a string.
    /// </summary>
    /// <param name="source">The <see cref="CordexSource"/> to convert.</param>
    /// <returns>The string representation of the <see cref="CordexSource"/>.</returns>
    public static string ToSourceId(this CordexSource source)
    {
        return source switch
        {
            CordexSource.BarpaR => barpaR,
            CordexSource.Ccamv2203SN => ccamv2203SN,
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
        };
    }

    /// <summary>
    /// Convert the source ID to the corresponding <see cref="CordexSource"/>.
    /// </summary>
    /// <param name="source">The source ID to convert.</param>
    /// <returns>The <see cref="CordexSource"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the source ID is not valid.</exception>
    public static CordexSource FromString(string source)
    {
        return source switch
        {
            barpaR => CordexSource.BarpaR,
            ccamv2203SN => CordexSource.Ccamv2203SN,
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
        };
    }
}


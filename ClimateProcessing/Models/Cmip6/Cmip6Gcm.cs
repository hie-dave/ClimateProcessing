namespace ClimateProcessing.Models.Cmip6;

/// <summary>
/// The GCM which produced a CMIP6 dataset.
/// </summary>
public enum Cmip6Gcm
{
    /// <summary>
    /// ACCESS-ESM1-5.
    /// </summary>
    AccessEsm15,

    /// <summary>
    /// NorESM2-MM.
    /// </summary>
    NorEsm2MM,

    /// <summary>
    /// EC-Earth3.
    /// </summary>
    ECEarth3
}

/// <summary>
/// Extension methods for the <see cref="Cmip6Gcm"/> enum.
/// </summary>
public static class Cmip6GcmExtensions
{
    private const string accessEsm15 = "ACCESS-ESM1-5";
    private const string norEsm2MM = "NorESM2-MM";
    private const string ecEarth3 = "EC-Earth3";

    /// <summary>
    /// Convert the specified GCM to its canonical string representation.
    /// </summary>
    /// <param name="gcm">The GCM.</param>
    /// <returns>The canonical string representation of the GCM.</returns>
    /// <exception cref="ArgumentException">Thrown when the experiment is not a valid <see cref="Cmip6Gcm"/>.</exception>
    public static string ToString(this Cmip6Gcm gcm) => gcm switch
    {
        Cmip6Gcm.AccessEsm15 => accessEsm15,
        Cmip6Gcm.NorEsm2MM => norEsm2MM,
        Cmip6Gcm.ECEarth3 => ecEarth3,
        _ => throw new ArgumentException($"Unknown CMIP6 GCM: {gcm}")
    };

    /// <summary>
    /// Convert a string to a <see cref="Cmip6Experiment"/>.
    /// </summary>
    /// <param name="gcmStr">The string to convert.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">Thrown when the string is not a valid <see cref="Cmip6Gcm"/>.</exception>
    public static Cmip6Gcm FromString(string gcmStr) => gcmStr switch
    {
        accessEsm15 => Cmip6Gcm.AccessEsm15,
        norEsm2MM => Cmip6Gcm.NorEsm2MM,
        ecEarth3 => Cmip6Gcm.ECEarth3,
        _ => throw new ArgumentException($"Unknown CMIP6 GCM string: {gcmStr}")
    };
}

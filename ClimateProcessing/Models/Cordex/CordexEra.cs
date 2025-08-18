namespace ClimateProcessing.Models.Cordex;

/// <summary>
/// The era of the CORDEX dataset.
/// </summary>
public enum CordexEra
{
    /// <summary>
    /// CMIP6 era.
    /// </summary>
    CMIP6
}

/// <summary>
/// Extension methods for the <see cref="CordexEra"/> enum.
/// </summary>
public static class CordexEraExtensions
{
    /// <summary>
    /// Convert the <see cref="CordexEra"/> to the corresponding era ID.
    /// </summary>
    /// <param name="era">The era to convert.</param>
    /// <returns>The era ID.</returns>
    public static string ToEraId(this CordexEra era)
    {
        return era switch
        {
            CordexEra.CMIP6 => "CMIP6",
            _ => throw new ArgumentOutOfRangeException(nameof(era), era, null)
        };
    }
}

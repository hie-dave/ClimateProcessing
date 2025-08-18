namespace ClimateProcessing.Models.Cordex;

/// <summary>
/// The domain of the CORDEX dataset.
/// </summary>
public enum CordexDomain
{
    /// <summary>
    /// Australian domain
    /// </summary>
    Aust05i
}

/// <summary>
/// Extension methods for the <see cref="CordexDomain"/> enum.
/// </summary>
public static class CordexDomainExtensions
{
    /// <summary>
    /// Convert the <see cref="CordexDomain"/> to the corresponding domain ID.
    /// </summary>
    /// <param name="domain">The domain to convert.</param>
    /// <returns>The domain ID.</returns>
    public static string ToDomainId(this CordexDomain domain)
    {
        return domain switch
        {
            CordexDomain.Aust05i => "AUST-05i",
            _ => throw new ArgumentOutOfRangeException(nameof(domain), domain, null)
        };
    }
}

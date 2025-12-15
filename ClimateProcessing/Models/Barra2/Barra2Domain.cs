namespace ClimateProcessing.Models.Barra2;

/// <summary>
/// The spatial domain and grid resolution of the BARRA2 data.
/// </summary>
public enum Barra2Domain
{
    /// <summary>
    /// AUS-11 domain.
    /// </summary>
    Aus11,

    /// <summary>
    /// AUS-22 domain.
    /// </summary>
    Aus22,

    /// <summary>
    /// AUST-04 domain.
    /// </summary>
    Aust04,

    /// <summary>
    /// AUST-11 domain.
    /// </summary>
    Aust11
}

/// <summary>
/// Extension methods for the <see cref="Barra2Domain"/> enum.
/// </summary>
public static class Barra2DomainExtensions
{
    /// <summary>
    /// Convert a <see cref="Barra2Domain"/> to a string.
    /// </summary>
    /// <param name="domain">The domain to convert.</param>
    /// <returns>The string representation of the domain.</returns>
    /// <exception cref="ArgumentException">Thrown when the domain is not a valid <see cref="Barra2Domain"/>.</exception>
    public static string ToString(this Barra2Domain domain) => domain switch
    {
        Barra2Domain.Aus11 => "AUS-11",
        Barra2Domain.Aus22 => "AUS-22",
        Barra2Domain.Aust04 => "AUST-04",
        Barra2Domain.Aust11 => "AUST-11",
        _ => throw new ArgumentException($"Unknown domain: {domain}")
    };

    /// <summary>
    /// Convert a string to a <see cref="Barra2Domain"/>.
    /// </summary>
    /// <param name="domainStr">The string to convert.</param>
    /// <returns>The <see cref="Barra2Domain"/> representation of the string.</returns>
    /// <exception cref="ArgumentException">Thrown when the string is not a valid <see cref="Barra2Domain"/>.</exception>
    public static Barra2Domain FromString(string domainStr) => domainStr switch
    {
        "AUS-11" => Barra2Domain.Aus11,
        "AUS-22" => Barra2Domain.Aus22,
        "AUST-04" => Barra2Domain.Aust04,
        "AUST-11" => Barra2Domain.Aust11,
        _ => throw new ArgumentException($"Unknown domain string: {domainStr}")
    };
}

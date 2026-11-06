namespace ClimateProcessing.Models.Barpa;

public enum BarpaDomain
{
    /// <summary>
    /// AUS-15
    /// </summary>
    Aus15,

    /// <summary>
    /// AUST-04
    /// </summary>
    Aust04,

    /// <summary>
    /// AUST-15
    /// </summary>
    Aust15,

    /// <summary>
    /// AUS-20i
    /// </summary>
    Aus20i
}

public static class BarpaDomainExtensions
{
    private const string aus15 = "AUS-15";
    private const string aust04 = "AUST-04";
    private const string aust15 = "AUST-15";
    private const string aus20i = "AUS-20i";

    /// <summary>
    /// Convert the <see cref="BarpaDomain"/> to the corresponding domain ID.
    /// </summary>
    /// <param name="domain">The BARPA domain.</param>
    /// <returns>The corresponding domain ID as a string.</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static string ToDomainId(this BarpaDomain domain)
    {
        return domain switch
        {
            BarpaDomain.Aus15 => aus15,
            BarpaDomain.Aust04 => aust04,
            BarpaDomain.Aust15 => aust15,
            BarpaDomain.Aus20i => aus20i,
            _ => throw new ArgumentOutOfRangeException(nameof(domain), domain, null)
        };
    }

    /// <summary>
    /// Convert a string to the corresponding <see cref="BarpaDomain"/> enum value.
    /// </summary>
    /// <param name="domain">The BARPA domain as a string.</param>
    /// <returns>The corresponding <see cref="BarpaDomain"/> enum value.</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static BarpaDomain FromString(string domain)
    {
        return domain switch
        {
            aus15 => BarpaDomain.Aus15,
            aust04 => BarpaDomain.Aust04,
            aust15 => BarpaDomain.Aust15,
            aus20i => BarpaDomain.Aus20i,
            _ => throw new ArgumentOutOfRangeException(nameof(domain), domain, null)
        };
    }

    /// <summary>
    /// Determine if the given domain is valid for the given project.
    /// </summary>
    /// <param name="domain">The BARPA domain.</param>
    /// <param name="project">The BARPA project.</param>
    /// <returns>True if the domain is valid for the project, false otherwise.</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static bool IsValidFor(this BarpaDomain domain, BarpaProject project)
    {
        return project switch
        {
            BarpaProject.CordexCmip6 => domain == BarpaDomain.Aus20i,
            BarpaProject.Output => domain == BarpaDomain.Aust04,
            // AUS-15, AUST-04, AUST-15
            BarpaProject.OutputCmip6 => domain != BarpaDomain.Aus20i,
            _ => throw new ArgumentOutOfRangeException(nameof(project), project, null)
        };
    }

    /// <summary>
    /// Get the source ID for the given domain and project.
    /// </summary>
    /// <param name="domain">The BARPA domain.</param>
    /// <param name="project">The BARPA project.</param>
    /// <returns>The source ID for the given domain and project.</returns>
    public static string GetSource(this BarpaDomain domain, BarpaProject project)
    {
        if (project == BarpaProject.Output ||
            (project == BarpaProject.OutputCmip6 && domain == BarpaDomain.Aust04))
            return "BARPA-C";

        // Everything else is BARPA-R.
        return "BARPA-R";
    }
}

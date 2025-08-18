namespace ClimateProcessing.Models.Cordex;

/// <summary>
/// The institution owning the dataset.
/// </summary>
public enum CordexInstitution
{
    /// <summary>
    /// The Australian Bureau of Meteorology.
    /// </summary>
    BOM,

    /// <summary>
    /// The Commonwealth Scientific and Industrial Research Organisation.
    /// </summary>
    CSIRO
}

/// <summary>
/// Extension methods for the <see cref="CordexInstitution"/> enum.
/// </summary>
public static class CordexInstitutionExtensions
{
    private const string bom = "BOM";
    private const string csiro = "CSIRO";

    /// <summary>
    /// Convert the <see cref="CordexInstitution"/> to the corresponding institution ID.
    /// </summary>
    /// <param name="institution">The institution to convert.</param>
    /// <returns>The institution ID.</returns>
    public static string ToInstitutionId(this CordexInstitution institution)
    {
        return institution switch
        {
            CordexInstitution.BOM => bom,
            CordexInstitution.CSIRO => csiro,
            _ => throw new ArgumentOutOfRangeException(nameof(institution), institution, null)
        };
    }

    /// <summary>
    /// Convert the institution ID to the corresponding <see cref="CordexInstitution"/>.
    /// </summary>
    /// <param name="institution">The institution ID to convert.</param>
    /// <returns>The <see cref="CordexInstitution"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the institution ID is not valid.</exception>
    public static CordexInstitution FromString(string institution)
    {
        return institution switch
        {
            bom => CordexInstitution.BOM,
            csiro => CordexInstitution.CSIRO,
            _ => throw new ArgumentOutOfRangeException(nameof(institution), institution, null)
        };
    }
}

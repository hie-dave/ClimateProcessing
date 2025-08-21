namespace ClimateProcessing.Models.Cordex;

public enum CordexVersion
{
    /// <summary>
    /// Original version (non-bias corrected)
    /// </summary>
    V1R1,

    /// <summary>
    /// V1-R1 + MRNBC bias correction on AGCD, 1960-2022
    /// </summary>
    MrnbcAgcd,

    /// <summary>
    /// V1-R1 + MRNBC bias correction on BARRA-R2, 1980-2022
    /// </summary>
    MrnbcBarra,

    /// <summary>
    /// V1-R1 + QME bias correction on AGCD, 1960-2022
    /// </summary>
    QmeAgcd,

    /// <summary>
    /// V1-R1 + QME bias correction on BARRA-R2, 1980-2022
    /// </summary>
    QmeBarra
}

/// <summary>
/// Extension methods for the <see cref="CordexVersion"/> enum.
/// </summary>
public static class CordexVersionExtensions
{
    private const string v1r1 = "v1-r1";
    private const string mrnbcAgcd = "v1-r1-ACS-MRNBC-AGCDv1-1960-2022";
    private const string mrnbcBarra = "v1-r1-ACS-MRNBC-BARRAR2-1980-2022";
    private const string qmeAgcd = "v1-r1-ACS-QME-AGCDv1-1960-2022";
    private const string qmeBarra = "v1-r1-ACS-QME-BARRAR2-1980-2022";

    /// <summary>
    /// Convert the <see cref="CordexVersion"/> to the corresponding version ID.
    /// </summary>
    /// <param name="version">The version to convert.</param>
    /// <returns>The version ID.</returns>
    public static string ToVersionId(this CordexVersion version)
    {
        return version switch
        {
            CordexVersion.V1R1 => v1r1,
            CordexVersion.MrnbcAgcd => mrnbcAgcd,
            CordexVersion.MrnbcBarra => mrnbcBarra,
            CordexVersion.QmeAgcd => qmeAgcd,
            CordexVersion.QmeBarra => qmeBarra,
            _ => throw new ArgumentOutOfRangeException(nameof(version), version, null)
        };
    }

    /// <summary>
    /// Convert the version ID to the corresponding <see cref="CordexVersion"/>.
    /// </summary>
    /// <param name="version">The version ID to convert.</param>
    /// <returns>The <see cref="CordexVersion"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the version ID is not valid.</exception>
    public static CordexVersion FromString(string version)
    {
        return version switch
        {
            v1r1 => CordexVersion.V1R1,
            mrnbcAgcd => CordexVersion.MrnbcAgcd,
            mrnbcBarra => CordexVersion.MrnbcBarra,
            qmeAgcd => CordexVersion.QmeAgcd,
            qmeBarra => CordexVersion.QmeBarra,
            _ => throw new ArgumentOutOfRangeException(nameof(version), version, null)
        };
    }

    public static bool IsSupportedFor(this CordexVersion version, CordexActivity activity)
    {
        // Only V1-R1 is supported for DD
        // All other versions are only supported for BiasCorrected
        return version switch
        {
            CordexVersion.V1R1 => activity == CordexActivity.DD,
            CordexVersion.MrnbcAgcd => activity != CordexActivity.DD,
            CordexVersion.MrnbcBarra => activity != CordexActivity.DD,
            CordexVersion.QmeAgcd => activity != CordexActivity.DD,
            CordexVersion.QmeBarra => activity != CordexActivity.DD,
            _ => throw new ArgumentOutOfRangeException(nameof(version), version, null)
        };
    }
}

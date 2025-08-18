namespace ClimateProcessing.Models.Cordex;

/// <summary>
/// The GCM of the dataset.
/// </summary>
public enum CordexGcm
{
    /// <summary>
    /// ACCESS-ESM2
    /// </summary>
    AccessCM2,

    /// <summary>
    /// ACCESS-ESM1-5
    /// </summary>
    AccessEsm15,

    /// <summary>
    /// CESM2
    /// </summary>
    Cesm2,

    /// <summary>
    /// CMCC-ESM2
    /// </summary>
    CmccEsm2,

    /// <summary>
    /// EC-Earth3
    /// </summary>
    ECEarth3,

    /// <summary>
    /// MPI-ESM1-2-HR
    /// </summary>
    MpiEsm12HR,

    /// <summary>
    /// NorESM2-MM
    /// </summary>
    NorEsm2MM
}

/// <summary>
/// Extension methods for the <see cref="CordexGcm"/> enum.
/// </summary>
public static class CordexGcmExtensions
{
    private const string accessCM2 = "ACCESS-ESM2";
    private const string accessEsm15 = "ACCESS-ESM1-5";
    private const string cesm2 = "CESM2";
    private const string cmccEsm2 = "CMCC-ESM2";
    private const string eCEarth3 = "EC-Earth3";
    private const string mpiEsm12HR = "MPI-ESM1-2-HR";
    private const string norEsm2MM = "NorESM2-MM";

    /// <summary>
    /// Convert the <see cref="CordexGcm"/> to the corresponding GCM ID.
    /// </summary>
    /// <param name="gcm">The GCM to convert.</param>
    /// <returns>The GCM ID.</returns>
    public static string ToGcmId(this CordexGcm gcm)
    {
        return gcm switch
        {
            CordexGcm.AccessCM2 => accessCM2,
            CordexGcm.AccessEsm15 => accessEsm15,
            CordexGcm.Cesm2 => cesm2,
            CordexGcm.CmccEsm2 => cmccEsm2,
            CordexGcm.ECEarth3 => eCEarth3,
            CordexGcm.MpiEsm12HR => mpiEsm12HR,
            CordexGcm.NorEsm2MM => norEsm2MM,
            _ => throw new ArgumentOutOfRangeException(nameof(gcm), gcm, null)
        };
    }

    /// <summary>
    /// Convert the GCM ID to the corresponding <see cref="CordexGcm"/>.
    /// </summary>
    /// <param name="gcm">The GCM ID to convert.</param>
    /// <returns>The <see cref="CordexGcm"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the GCM ID is not valid.</exception>
    public static CordexGcm FromString(string gcm)
    {
        return gcm switch
        {
            accessCM2 => CordexGcm.AccessCM2,
            accessEsm15 => CordexGcm.AccessEsm15,
            cesm2 => CordexGcm.Cesm2,
            cmccEsm2 => CordexGcm.CmccEsm2,
            eCEarth3 => CordexGcm.ECEarth3,
            mpiEsm12HR => CordexGcm.MpiEsm12HR,
            norEsm2MM => CordexGcm.NorEsm2MM,
            _ => throw new ArgumentOutOfRangeException(nameof(gcm), gcm, null)
        };
    }

    /// <summary>
    /// Get the variant label for the GCM.
    /// </summary>
    /// <param name="gcm">The GCM to get the variant label for.</param>
    /// <returns>The variant label.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an invalid GCM is specified.</exception>
    public static string GetVariantLabel(this CordexGcm gcm)
    {
        return gcm switch
        {
            CordexGcm.AccessCM2 => "r4i1p1f1",
            CordexGcm.AccessEsm15 => "r6i1p1f1",
            CordexGcm.Cesm2 => "r11i1p1f1",
            CordexGcm.CmccEsm2 => "r1i1p1f1",
            CordexGcm.ECEarth3 => "r1i1p1f1",
            CordexGcm.MpiEsm12HR => "r1i1p1f1",
            CordexGcm.NorEsm2MM => "r1i1p1f1",
            _ => throw new ArgumentOutOfRangeException(nameof(gcm), gcm, null)
        };
    }
}

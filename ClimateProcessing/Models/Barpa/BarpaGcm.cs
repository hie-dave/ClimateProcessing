namespace ClimateProcessing.Models.Barpa;

public enum BarpaGcm
{
    Era5,
    AccessCM2,
    AccessEsm15,
    NorEsm2MM,
    ECEarth3,
    Cesm2,
    CmccEsm2,
    MpiEsm12HR
}

public static class BarpaGcmExtensions
{
    private const string era5 = "ERA5";
    private const string accessCM2 = "ACCESS-CM2";
    private const string accessEsm15 = "ACCESS-ESM1-5";
    private const string norEsm2MM = "NorESM2-MM";
    private const string ecEarth3 = "EC-Earth3";
    private const string cesm2 = "CESM2";
    private const string cmccEsm2 = "CMCC-ESM2";
    private const string mpiEsm12HR = "MPI-ESM1-2-HR";

    private static readonly BarpaGcm[] validOutputCmip6Aust04Domains = [
        BarpaGcm.AccessEsm15,
        BarpaGcm.ECEarth3,
        BarpaGcm.Era5];

    public static string ToGcmId(this BarpaGcm gcm)
    {
        return gcm switch
        {
            BarpaGcm.Era5 => era5,
            BarpaGcm.AccessCM2 => accessCM2,
            BarpaGcm.AccessEsm15 => accessEsm15,
            BarpaGcm.NorEsm2MM => norEsm2MM,
            BarpaGcm.ECEarth3 => ecEarth3,
            BarpaGcm.Cesm2 => cesm2,
            BarpaGcm.CmccEsm2 => cmccEsm2,
            BarpaGcm.MpiEsm12HR => mpiEsm12HR,
            _ => throw new ArgumentOutOfRangeException(nameof(gcm), gcm, null)
        };
    }

    public static BarpaGcm FromString(string gcm)
    {
        return gcm switch
        {
            era5 => BarpaGcm.Era5,
            accessCM2 => BarpaGcm.AccessCM2,
            accessEsm15 => BarpaGcm.AccessEsm15,
            norEsm2MM => BarpaGcm.NorEsm2MM,
            ecEarth3 => BarpaGcm.ECEarth3,
            cesm2 => BarpaGcm.Cesm2,
            cmccEsm2 => BarpaGcm.CmccEsm2,
            mpiEsm12HR => BarpaGcm.MpiEsm12HR,
            _ => throw new ArgumentOutOfRangeException(nameof(gcm), gcm, null)
        };
    }

    public static bool IsValidFor(this BarpaGcm gcm,
                                  BarpaProject project,
                                  BarpaDomain domain)
    {
        if (project == BarpaProject.CordexCmip6)
            return true; // all supported for CORDEX-CMIP6

        if (project == BarpaProject.Output)
            return gcm == BarpaGcm.Era5;

        // output-CMIP6 x AUST-04 is only valid for a few GCMs.
        if (domain == BarpaDomain.Aust04)
            return validOutputCmip6Aust04Domains.Contains(gcm);

        return true; // all supported for output-CMIP6 AUS-15 and AUST-15
    }

    public static string GetVariantLabel(this BarpaGcm gcm)
    {
        return gcm switch
        {
            BarpaGcm.AccessCM2 => "r4i1p1f1",
            BarpaGcm.AccessEsm15 => "r6i1p1f1",
            BarpaGcm.Cesm2 => "r11i1p1f1",
            BarpaGcm.CmccEsm2 => "r1i1p1f1",
            BarpaGcm.ECEarth3 => "r1i1p1f1",
            BarpaGcm.MpiEsm12HR => "r1i1p1f1",
            BarpaGcm.NorEsm2MM => "r1i1p1f1",
            BarpaGcm.Era5 => "r1i1p1f1",
            _ => throw new ArgumentOutOfRangeException(nameof(gcm), gcm, null)
        };
    }
}

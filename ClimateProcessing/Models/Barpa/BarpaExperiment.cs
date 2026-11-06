using ClimateProcessing.Models.Barpa;

namespace ClimateProcessing.Models;

public enum BarpaExperiment
{
    Historical,
    Ssp126,
    Ssp370,
    Ssp585,
    Evaluation
}

public static class BarpaExperimentExtensions
{
    private const string historical = "historical";
    private const string ssp126 = "ssp126";
    private const string ssp370 = "ssp370";
    private const string ssp585 = "ssp585";
    private const string evaluation = "evaluation";

    public static string ToExperimentId(this BarpaExperiment experiment)
    {
        return experiment switch
        {
            BarpaExperiment.Historical => historical,
            BarpaExperiment.Ssp126 => ssp126,
            BarpaExperiment.Ssp370 => ssp370,
            BarpaExperiment.Ssp585 => ssp585,
            BarpaExperiment.Evaluation => evaluation,
            _ => throw new ArgumentOutOfRangeException(nameof(experiment), experiment, null)
        };
    }

    public static BarpaExperiment FromString(string experiment)
    {
        return experiment switch
        {
            historical => BarpaExperiment.Historical,
            ssp126 => BarpaExperiment.Ssp126,
            ssp370 => BarpaExperiment.Ssp370,
            ssp585 => BarpaExperiment.Ssp585,
            evaluation => BarpaExperiment.Evaluation,
            _ => throw new ArgumentOutOfRangeException(nameof(experiment), experiment, null)
        };
    }

    public static bool IsValidFor(this BarpaExperiment experiment,
                                  BarpaGcm gcm,
                                  BarpaProject project,
                                  BarpaDomain domain)
    {
        if (gcm == BarpaGcm.Era5)
            return experiment == BarpaExperiment.Evaluation;

        if (experiment == BarpaExperiment.Evaluation)
            return false;

        if (project == BarpaProject.CordexCmip6)
        {
            if (experiment == BarpaExperiment.Ssp585)
                return gcm == BarpaGcm.AccessCM2 || gcm == BarpaGcm.ECEarth3;

            // historical, ssp126, and ssp370 are valid for all GCMs except ERA5.
            return true;
        }

        if (project == BarpaProject.Output)
            return gcm == BarpaGcm.Era5 && experiment == BarpaExperiment.Evaluation;

        // output-CMIP6 is a mixed bag.
        if (domain == BarpaDomain.Aus15)
        {
            if (experiment == BarpaExperiment.Ssp585)
                return gcm == BarpaGcm.AccessCM2 || gcm == BarpaGcm.ECEarth3;

            // historical, ssp126, and ssp370 are valid for all GCMs except ERA5.
            return true;
        }

        if (domain == BarpaDomain.Aust04)
        {
            return experiment == BarpaExperiment.Historical ||
                   experiment == BarpaExperiment.Ssp370;
        }

        // output-CMIP6 x AUST-15

        if (experiment == BarpaExperiment.Ssp585)
            return gcm == BarpaGcm.AccessCM2 || gcm == BarpaGcm.ECEarth3;

        // historical, ssp126, and ssp370 are valid for all GCMs except ERA5.
        return true;
    }
}

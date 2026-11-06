namespace ClimateProcessing.Models.Barpa;

/// <summary>
/// The time frequency of the BARPA data.
/// </summary>
public enum BarpaFrequency
{
    /// <summary>
    /// 1-hourly data.
    /// </summary>
    Hourly,

    /// <summary>
    /// 3-hourly data.
    /// </summary>
    ThreeHourly,

    /// <summary>
    /// 6-hourly data.
    /// </summary>
    SixHourly,

    /// <summary>
    /// Daily data.
    /// </summary>
    Daily,

    /// <summary>
    /// Monthly data.
    /// </summary>
    Monthly
}

public static class BarpaFrequencyExtensions
{
    private const string hourly = "1hr";
    private const string threeHourly = "3hr";
    private const string sixHourly = "6hr";
    private const string daily = "day";
    private const string monthly = "mon";

    public static string ToFrequencyId(this BarpaFrequency frequency)
    {
        return frequency switch
        {
            BarpaFrequency.Hourly => hourly,
            BarpaFrequency.ThreeHourly => threeHourly,
            BarpaFrequency.SixHourly => sixHourly,
            BarpaFrequency.Daily => daily,
            BarpaFrequency.Monthly => monthly,
            _ => throw new ArgumentOutOfRangeException(nameof(frequency), frequency, null)
        };
    }

    public static BarpaFrequency FromString(string frequency)
    {
        return frequency switch
        {
            hourly => BarpaFrequency.Hourly,
            threeHourly => BarpaFrequency.ThreeHourly,
            sixHourly => BarpaFrequency.SixHourly,
            daily => BarpaFrequency.Daily,
            monthly => BarpaFrequency.Monthly,
            _ => throw new ArgumentOutOfRangeException(nameof(frequency), frequency, null)
        };
    }

    public static bool IsValidFor(this BarpaFrequency frequency, BarpaProject project)
    {
        if (project == BarpaProject.CordexCmip6)
        {
            return frequency == BarpaFrequency.ThreeHourly ||
                   frequency == BarpaFrequency.Daily ||
                   frequency == BarpaFrequency.Monthly;
        }

        if (project == BarpaProject.Output)
        {
            return frequency == BarpaFrequency.Hourly ||
                   frequency == BarpaFrequency.ThreeHourly ||
                   frequency == BarpaFrequency.SixHourly ||
                   frequency == BarpaFrequency.Daily ||
                   frequency == BarpaFrequency.Monthly;
        }

        // output-CMIP6 project

        return frequency == BarpaFrequency.Hourly ||
               frequency == BarpaFrequency.SixHourly ||
               frequency == BarpaFrequency.Daily ||
               frequency == BarpaFrequency.Monthly;
    }
}

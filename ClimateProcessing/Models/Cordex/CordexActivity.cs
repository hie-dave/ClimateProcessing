namespace ClimateProcessing.Models.Cordex;

/// <summary>
/// The activity of the CORDEX dataset.
/// </summary>
public enum CordexActivity
{
    /// <summary>
    /// Dynamic Downscaling (ie the regridded data).
    /// </summary>
    DD,

    /// <summary>
    /// Bias-corrected output
    /// </summary>
    BiasCorrected
}

/// <summary>
/// Extension methods for the <see cref="CordexActivity"/> enum.
/// </summary>
public static class CordexActivityExtensions
{
    private const string DD = "DD";
    private const string BiasCorrected = "bias-adjusted-output";

    /// <summary>
    /// Convert the <see cref="CordexActivity"/> to the corresponding activity ID.
    /// </summary>
    /// <param name="activity">The activity to convert.</param>
    /// <returns>The activity ID.</returns>
    public static string ToActivityId(this CordexActivity activity)
    {
        return activity switch
        {
            CordexActivity.DD => DD,
            CordexActivity.BiasCorrected => BiasCorrected,
            _ => throw new ArgumentOutOfRangeException(nameof(activity), activity, null)
        };
    }

    /// <summary>
    /// Convert the activity ID to the corresponding <see cref="CordexActivity"/>.
    /// </summary>
    /// <param name="activity">The activity ID to convert.</param>
    /// <returns>The <see cref="CordexActivity"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the activity ID is not valid.</exception>
    public static CordexActivity FromString(string activity)
    {
        return activity switch
        {
            DD => CordexActivity.DD,
            BiasCorrected => CordexActivity.BiasCorrected,
            _ => throw new ArgumentOutOfRangeException(nameof(activity), activity, null)
        };
    }
}


namespace ClimateProcessing.Models.Cordex;

/// <summary>
/// The frequency of the dataset.
/// </summary>
public enum CordexFrequency
{
    /// <summary>
    /// Daily
    /// </summary>
    Daily
}

/// <summary>
/// Extension methods for the <see cref="CordexFrequency"/> enum.
/// </summary>
public static class CordexFrequencyExtensions
{
    /// <summary>
    /// Converts the <see cref="CordexFrequency"/> to a string.
    /// </summary>
    /// <param name="frequency">The <see cref="CordexFrequency"/> to convert.</param>
    /// <returns>The string representation of the <see cref="CordexFrequency"/>.</returns>
    public static string ToFrequencyId(this CordexFrequency frequency)
    {
        return frequency switch
        {
            CordexFrequency.Daily => "day",
            _ => throw new ArgumentOutOfRangeException(nameof(frequency), frequency, null)
        };
    }
}

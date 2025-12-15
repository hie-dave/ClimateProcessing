namespace ClimateProcessing.Models.Barra2;

/// <summary>
/// The time frequency of the BARRA2 data.
/// </summary>
public enum Barra2Frequency
{
    /// <summary>
    /// 1-hourly data.
    /// </summary>
    Hour1,

    /// <summary>
    /// 3-hourly data.
    /// </summary>
    Hour3,

    /// <summary>
    /// 6-hourly data.
    /// </summary>
    Hour6,

    /// <summary>
    /// Daily data.
    /// </summary>
    Daily,

    /// <summary>
    /// Monthly data.
    /// </summary>
    Monthly,

    /// <summary>
    /// Constant data.
    /// </summary>
    Constant
}

/// <summary>
/// Extension methods for the <see cref="Barra2Frequency"/> enum.
/// </summary>
public static class Barra2FrequencyExtensions
{
    /// <summary>
    /// The string representation of the <see cref="Barra2Frequency.Hour1"/> frequency.
    /// </summary>
    private const string hour1 = "1hr";

    /// <summary>
    /// The string representation of the <see cref="Barra2Frequency.Hour3"/> frequency.
    /// </summary>
    private const string hour3 = "3hr";

    /// <summary>
    /// The string representation of the <see cref="Barra2Frequency.Hour6"/> frequency.
    /// </summary>
    private const string hour6 = "6hr";

    /// <summary>
    /// The string representation of the <see cref="Barra2Frequency.Daily"/> frequency.
    /// </summary>
    private const string daily = "day";

    /// <summary>
    /// The string representation of the <see cref="Barra2Frequency.Monthly"/> frequency.
    /// </summary>
    private const string monthly = "mon";

    /// <summary>
    /// The string representation of the <see cref="Barra2Frequency.Constant"/> frequency.
    /// </summary>
    private const string constant = "fx";

    /// <summary>
    /// Convert a <see cref="Barra2Frequency"/> to a string.
    /// </summary>
    /// <param name="frequency">The frequency to convert.</param>
    /// <returns>The string representation of the frequency.</returns>
    /// <exception cref="ArgumentException">Thrown when the frequency is not a valid <see cref="Barra2Frequency"/>.</exception>
    public static string ToString(this Barra2Frequency frequency) => frequency switch
    {
        Barra2Frequency.Hour1 => hour1,
        Barra2Frequency.Hour3 => hour3,
        Barra2Frequency.Hour6 => hour6,
        Barra2Frequency.Daily => daily,
        Barra2Frequency.Monthly => monthly,
        Barra2Frequency.Constant => constant,
        _ => throw new ArgumentException($"Unknown frequency: {frequency}")
    };

    /// <summary>
    /// Convert a string to a <see cref="Barra2Frequency"/>.
    /// </summary>
    /// <param name="frequencyStr">The string to convert.</param>
    /// <returns>The <see cref="Barra2Frequency"/> representation of the string.</returns>
    /// <exception cref="ArgumentException">Thrown when the string is not a valid <see cref="Barra2Frequency"/>.</exception>
    public static Barra2Frequency FromString(string frequencyStr) => frequencyStr switch
    {
        hour1 => Barra2Frequency.Hour1,
        hour3 => Barra2Frequency.Hour3,
        hour6 => Barra2Frequency.Hour6,
        daily => Barra2Frequency.Daily,
        monthly => Barra2Frequency.Monthly,
        constant => Barra2Frequency.Constant,
        _ => throw new ArgumentException($"Unknown frequency string: {frequencyStr}")
    };
}


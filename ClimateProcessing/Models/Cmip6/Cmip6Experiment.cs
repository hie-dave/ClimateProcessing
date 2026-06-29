namespace ClimateProcessing.Models.Cmip6;

/// <summary>
/// A CMIP6 scenario/experiment.
/// </summary>
public enum Cmip6Experiment
{
    /// <summary>
    /// SSP1-1.9.
    /// </summary>
    Ssp119,
    /// <summary>
    /// SSP2-4.5.
    /// </summary>
    Ssp245
}

/// <summary>
/// Extension methods for the <see cref="Cmip6Experiment"/> enum.
/// </summary>
public static class Cmip6ExperimentExtensions
{
    private const string ssp119 = "ssp119";
    private const string ssp245 = "ssp245";

    /// <summary>
    /// Convert an experiment to its canonical string representation.
    /// </summary>
    /// <param name="expt">A CMIP6 experiment.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">Thrown when the experiment is not a valid <see cref="Cmip6Experiment"/>.</exception>
    public static string ToString(this Cmip6Experiment expt) => expt switch
    {
        Cmip6Experiment.Ssp119 => ssp119,
        Cmip6Experiment.Ssp245 => ssp245,
        _ => throw new ArgumentException($"Unknown CMIP6 experiment: {expt}")
    };

    /// <summary>
    /// Convert a string to a <see cref="Cmip6Experiment"/>.
    /// </summary>
    /// <param name="str">The string to convert.</param>
    /// <returns>The <see cref="Cmip6Experiment"/> representation of the string.</returns>
    /// <exception cref="ArgumentException">Thrown when the string is not a valid <see cref="Cmip6Experiment"/>.</exception>
    public static Cmip6Experiment FromString(string str) => str switch
    {
        ssp119 => Cmip6Experiment.Ssp119,
        ssp245 => Cmip6Experiment.Ssp245,
        _ => throw new ArgumentException($"Unknokwn CMIP6 experiment: {str}"),
    };
}

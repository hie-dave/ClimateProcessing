namespace ClimateProcessing.Models.Cordex;

/// <summary>
/// The experiment of the dataset.
/// </summary>
public enum CordexExperiment
{
    /// <summary>
    /// Historical experiment.
    /// </summary>
    Historical,

    /// <summary>
    /// SSP1-2.6 experiment.
    /// </summary>
    Ssp126,

    /// <summary>
    /// SSP3-7.0 experiment.
    /// </summary>
    Ssp370
}

/// <summary>
/// Extension methods for the <see cref="CordexExperiment"/> enum.
/// </summary>
public static class CordexExperimentExtensions
{
    private const string historical = "historical";
    private const string ssp126 = "ssp126";
    private const string ssp370 = "ssp370";

    /// <summary>
    /// Convert the <see cref="CordexExperiment"/> to the corresponding experiment ID.
    /// </summary>
    /// <param name="experiment">The experiment to convert.</param>
    /// <returns>The experiment ID.</returns>
    public static string ToExperimentId(this CordexExperiment experiment)
    {
        return experiment switch
        {
            CordexExperiment.Historical => historical,
            CordexExperiment.Ssp126 => ssp126,
            CordexExperiment.Ssp370 => ssp370,
            _ => throw new ArgumentOutOfRangeException(nameof(experiment), experiment, null)
        };
    }

    /// <summary>
    /// Convert the experiment ID to the corresponding <see cref="CordexExperiment"/>.
    /// </summary>
    /// <param name="experiment">The experiment ID to convert.</param>
    /// <returns>The <see cref="CordexExperiment"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the experiment ID is not valid.</exception>
    public static CordexExperiment FromString(string experiment)
    {
        return experiment switch
        {
            historical => CordexExperiment.Historical,
            ssp126 => CordexExperiment.Ssp126,
            ssp370 => CordexExperiment.Ssp370,
            _ => throw new ArgumentOutOfRangeException(nameof(experiment), experiment, null)
        };
    }
}

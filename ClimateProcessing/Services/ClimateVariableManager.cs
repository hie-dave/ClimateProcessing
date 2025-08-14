using ClimateProcessing.Models;
using ClimateProcessing.Units;

namespace ClimateProcessing.Services;

/// <summary>
/// Manages climate variable requirements for different model versions.
/// </summary>
public class ClimateVariableManager
{
    /// <summary>
    /// List of standard variables and their output names and units.
    /// </summary>
    private static readonly Dictionary<ClimateVariable, string> outputNames = new()
    {
        { ClimateVariable.SpecificHumidity, "huss" }, // "1"
        { ClimateVariable.SurfacePressure, "ps" }, // "Pa"
        { ClimateVariable.ShortwaveRadiation, "rsds" }, // "W m-2"
        { ClimateVariable.WindSpeed, "sfcWind" }, // "m s-1"
        { ClimateVariable.Temperature, "tas" }, // "degC"
        { ClimateVariable.Precipitation, "pr" }, // "mm"
        { ClimateVariable.MaxTemperature, "tasmax" }, // "degC"
        { ClimateVariable.MinTemperature, "tasmin" }, // "degC"
    };

    /// <summary>
    /// Aggregation methods for each variable.
    /// </summary>
    private static readonly IReadOnlyDictionary<ClimateVariable, AggregationMethod> aggregationMethods = new Dictionary<ClimateVariable, AggregationMethod>()
    {
        { ClimateVariable.Temperature, AggregationMethod.Mean },
        { ClimateVariable.Precipitation, AggregationMethod.Sum },
        { ClimateVariable.SpecificHumidity, AggregationMethod.Mean },
        { ClimateVariable.SurfacePressure, AggregationMethod.Mean },
        { ClimateVariable.ShortwaveRadiation, AggregationMethod.Mean },
        { ClimateVariable.WindSpeed, AggregationMethod.Mean },
        { ClimateVariable.MaxTemperature, AggregationMethod.Maximum },
        { ClimateVariable.MinTemperature, AggregationMethod.Minimum },
    };

    /// <summary>
    /// Units for each variable in the Dave version.
    /// </summary>
    private static readonly IReadOnlyDictionary<ClimateVariable, string> daveUnits = new Dictionary<ClimateVariable, string>()
    {
        { ClimateVariable.Temperature, "degC" },
        { ClimateVariable.Precipitation, "mm" },
        { ClimateVariable.SpecificHumidity, "1" },
        { ClimateVariable.SurfacePressure, "Pa" },
        { ClimateVariable.ShortwaveRadiation, "W m-2" },
        { ClimateVariable.WindSpeed, "m s-1" }
    };

    /// <summary>
    /// Units for each variable in the trunk version.
    /// </summary>
    private static readonly IReadOnlyDictionary<ClimateVariable, string> trunkUnits = new Dictionary<ClimateVariable, string>()
    {
        { ClimateVariable.Temperature, "K" },
        { ClimateVariable.Precipitation, "mm" },
        { ClimateVariable.SpecificHumidity, "1" },
        { ClimateVariable.SurfacePressure, "Pa" },
        { ClimateVariable.ShortwaveRadiation, "W m-2" },
        { ClimateVariable.WindSpeed, "m s-1" },
        { ClimateVariable.MaxTemperature, "K" },
        { ClimateVariable.MinTemperature, "K" },
    };

    /// <summary>
    /// The version of LPJ-Guess by which the data is going to be used.
    /// </summary>
    private readonly ModelVersion version;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClimateVariableManager"/> class.
    /// </summary>
    /// <param name="version">The version of LPJ-Guess by which the data is going to be used.</param>
    public ClimateVariableManager(ModelVersion version)
    {
        this.version = version;
    }

    /// <summary>
    /// Gets the output requirements for the specified variable.
    /// </summary>
    /// <param name="variable">The variable.</param>
    /// <returns>The output requirements.</returns>
    /// <exception cref="ArgumentException">If no configuration is found for the specified variable.</exception>
    public VariableInfo GetOutputRequirements(ClimateVariable variable)
    {
        if (!outputNames.TryGetValue(variable, out string? outName))
            throw new ArgumentException($"No configuration found for variable {variable}");
        string outUnits = GetTargetUnits(variable);
        return new VariableInfo(outName, outUnits);
    }

    /// <summary>
    /// Gets the set of variables required by the configured model version.
    /// </summary>
    /// <returns>Collection of ClimateVariable values.</returns>
    public IEnumerable<ClimateVariable> GetRequiredVariables()
    {
        return GetDictionary().Keys;
    }

    /// <summary>
    /// Get the aggregation method required for the processing of the specified variable.
    /// </summary>
    /// <param name="variable">The variable.</param>
    /// <returns>The aggregation method.</returns>
    /// <exception cref="ArgumentException">If no configuration is found for the specified variable.</exception>
    public static AggregationMethod GetAggregationMethod(ClimateVariable variable)
    {
        return aggregationMethods[variable];
    }

    /// <summary>
    /// Gets the units of the specified variable required by the model version.
    /// </summary>
    /// <param name="variable">The variable.</param>
    /// <returns>The target units.</returns>
    /// <exception cref="ArgumentException">If no configuration is found for the specified variable.</exception>
    private string GetTargetUnits(ClimateVariable variable)
    {
        IReadOnlyDictionary<ClimateVariable, string> dict = GetDictionary();
        if (!dict.TryGetValue(variable, out string? units))
            throw new ArgumentException($"No configuration found for variable {variable}");
        return units;
    }

    /// <summary>
    /// Get the unit lookup dictionary corresponding to the configured model
    /// version.
    /// </summary>
    /// <returns>
    /// A dictionary mapping climate variables to the units required by the
    /// configured model version.
    /// </returns>
    private IReadOnlyDictionary<ClimateVariable, string> GetDictionary()
    {
        return version == ModelVersion.Dave ? daveUnits : trunkUnits;
    }
}

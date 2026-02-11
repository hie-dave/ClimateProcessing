using ClimateProcessing.Models;
using ClimateProcessing.Units;

namespace ClimateProcessing.Services;

/// <summary>
/// Manages climate variable requirements for different model versions.
/// </summary>
public class ClimateVariableManager : IClimateVariableManager
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
        { ClimateVariable.RelativeHumidity, "hurs" }, // "1" (trunk only)
        { ClimateVariable.MinRelativeHumidity, "hursmin" }, // "1" (intermediate output only)
        { ClimateVariable.MaxRelativeHumidity, "hursmax" }, // "1" (intermediate output only)
        { ClimateVariable.Vpd, "vpd" }, // "kPa" (dave only)
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
        { ClimateVariable.RelativeHumidity, AggregationMethod.Mean },
        { ClimateVariable.MinRelativeHumidity, AggregationMethod.Mean },
        { ClimateVariable.MaxRelativeHumidity, AggregationMethod.Mean },
        { ClimateVariable.Vpd, AggregationMethod.Mean },
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
        { ClimateVariable.WindSpeed, "m s-1" },
        { ClimateVariable.Vpd, "kPa" },
        // Dave doesn't technically use min/max daily temperature itself, but
        // the MAESPA weather generator does require these (in ℃), so we
        // set these here.
        { ClimateVariable.MaxTemperature, "degC" },
        { ClimateVariable.MinTemperature, "degC" },
    };

    /// <summary>
    /// Units for each variable in the trunk version.
    /// </summary>
    private static readonly IReadOnlyDictionary<ClimateVariable, string> trunkUnits = new Dictionary<ClimateVariable, string>()
    {
        { ClimateVariable.Temperature, "K" },
        { ClimateVariable.Precipitation, "kg m-2" },
        { ClimateVariable.SpecificHumidity, "1" },
        { ClimateVariable.SurfacePressure, "Pa" },
        { ClimateVariable.ShortwaveRadiation, "W m-2" },
        { ClimateVariable.WindSpeed, "m s-1" },
        { ClimateVariable.MaxTemperature, "K" },
        { ClimateVariable.MinTemperature, "K" },
        { ClimateVariable.RelativeHumidity, "1" },
        { ClimateVariable.MinRelativeHumidity, "1" },
        { ClimateVariable.MaxRelativeHumidity, "1" }
    };

    /// <summary>
    /// Standard names for each variable as required by the model. Only the
    /// trunk version is really "strict" about the standard names, so we just
    /// use this same lookup table for the dave version too (as it results in
    /// more intuitive output files).
    /// </summary>
    private static readonly IReadOnlyDictionary<ClimateVariable, string> standardNames = new Dictionary<ClimateVariable, string>()
    {
        { ClimateVariable.Temperature, "air_temperature" },
        // "precipitation_flux" is accepted if the units are "kg m-2 s-1", but
        // we don't support this use case (nor do we need to).
        { ClimateVariable.Precipitation, "precipitation_amount" },
        { ClimateVariable.SpecificHumidity, "specific_humidity" },
        { ClimateVariable.SurfacePressure, "surface_air_pressure" },
        // Several valid options exist for shortwave radiation.
        { ClimateVariable.ShortwaveRadiation, "surface_downwelling_shortwave_flux_in_air" },
        { ClimateVariable.WindSpeed, "wind_speed" },
        { ClimateVariable.RelativeHumidity, "relative_humidity" },
        { ClimateVariable.MaxTemperature, "air_temperature_maximum" },
        { ClimateVariable.MinTemperature, "air_temperature_minimum" },
        { ClimateVariable.MaxRelativeHumidity, "relative_humidity_maximum" },
        { ClimateVariable.MinRelativeHumidity, "relative_humidity_minimum" },
        { ClimateVariable.Vpd, "water_vapor_saturation_deficit_in_air" },
    };

    /// <summary>
    /// Long names for each variable as required by the model.
    /// </summary>
    /// <remarks>
    /// No version of the model is strict about the long names; these are
    /// included for metadata purposes only.
    /// </remarks>
    private static readonly Dictionary<ClimateVariable, string> longNames = new()
    {
        { ClimateVariable.Temperature, "Air Temperature" },
        { ClimateVariable.Precipitation, "Precipitation" },
        { ClimateVariable.SpecificHumidity, "Specific Humidity" },
        { ClimateVariable.SurfacePressure, "Surface Air Pressure" },
        { ClimateVariable.ShortwaveRadiation, "Down-welling Shortwave Radiation" },
        { ClimateVariable.WindSpeed, "Wind Speed" },
        { ClimateVariable.RelativeHumidity, "Relative Humidity" },
        { ClimateVariable.MaxTemperature, "Maximum Air Temperature" },
        { ClimateVariable.MinTemperature, "Minimum Air Temperature" },
        { ClimateVariable.MaxRelativeHumidity, "Maximum Relative Humidity" },
        { ClimateVariable.MinRelativeHumidity, "Minimum Relative Humidity" },
        { ClimateVariable.Vpd, "Vapor Pressure Deficit" },
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

    /// <inheritdoc/>
    public VariableInfo GetOutputRequirements(ClimateVariable variable)
    {
        if (!outputNames.TryGetValue(variable, out string? outName))
            throw new ArgumentException($"No output requirements defined for variable {variable}");
        string outUnits = GetTargetUnits(variable);
        return new VariableInfo(outName, outUnits);
    }

    /// <inheritdoc/>
    public IEnumerable<ClimateVariable> GetRequiredVariables()
    {
        return GetDictionary().Keys;
    }

    /// <inheritdoc/>
    public AggregationMethod GetAggregationMethod(ClimateVariable variable)
    {
        if (!aggregationMethods.TryGetValue(variable, out AggregationMethod method))
            throw new ArgumentException($"No aggregation method defined for variable {variable}");
        return method;
    }

    /// <inheritdoc/>
    public VariableMetadata GetMetadata(ClimateVariable variable)
    {
        string standardName = GetStandardName(variable);
        string longName = GetLongName(variable);
        string units = GetTargetUnits(variable);
        return new VariableMetadata(standardName, longName, units);
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
            throw new ArgumentException($"No unit requirements defined for variable {variable} in version {version}");
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

    /// <summary>
    /// Gets the long name of the specified variable.
    /// </summary>
    /// <param name="variable">The variable.</param>
    /// <returns>The long name of the variable.</returns>
    /// <exception cref="ArgumentException">If no configuration is found for the specified variable.</exception>
    private static string GetLongName(ClimateVariable variable)
    {
        if (!longNames.TryGetValue(variable, out string? longName))
            throw new ArgumentException($"No long name defined for variable {variable}");
        return longName;
    }

    /// <summary>
    /// Gets the standard name of the specified variable.
    /// </summary>
    /// <param name="variable">The variable.</param>
    /// <returns>The standard name of the variable.</returns>
    /// <exception cref="ArgumentException">If no configuration is found for the specified variable.</exception>
    private static string GetStandardName(ClimateVariable variable)
    {
        if (!standardNames.TryGetValue(variable, out string? standardName))
            throw new ArgumentException($"No standard name defined for variable {variable}");
        return standardName;
    }
}

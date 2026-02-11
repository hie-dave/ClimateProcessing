namespace ClimateProcessing.Units;

/// <summary>
/// A function that generates a CDO operator which performs a unit
/// conversion.
/// </summary>
/// <param name="timestep">The number of seconds in a time step.</param>
/// <returns>A CDO operator which performs the unit conversion.</returns>
public delegate string ConversionDefinition(int timestep);

/// <summary>
/// Converts between units.
/// </summary>
public static class UnitConverter
{
    /// <summary>
    /// 0 °C in Kelvin.
    /// </summary>
    private const double degCToK = 273.15;

    /// <summary>
    /// A dictionary mapping canonical unit names to sets of synonyms/aliases.
    /// </summary>
    private static readonly Dictionary<string, HashSet<string>> UnitSynonyms = new()
    {
        ["1"] = new() { "kg/kg", "kg kg-1", "1" },
        ["W/m2"] = new() { "W/m2", "W/m^2", "W m-2" },
        ["degC"] = new() { "degC", "°C", "Celsius" },
        ["K"] = new() { "K", "Kelvin" },
        ["mm"] = new() { "mm", "kg m-2" }  // 1mm of water = 1 kg/m2
    };

    /// <summary>
    /// A dictionary of unit conversion expressions.
    /// </summary>
    private static readonly Dictionary<(string From, string To), ConversionDefinition> ConversionExpressions = new()
    {
        [("K", "degC")] = _ => $"-subc,{degCToK}",
        [("degC", "K")] = _ => $"-addc,{degCToK}",
        // Multiply by seconds in period to get accumulation
        [("kg m-2 s-1", "mm")] = t => $"-mulc,{t}",
        [("kPa", "Pa")] = _ => "-mulc,1000",
        [("%", "1")] = _ => "-divc,100",
        // Divide by seconds in day to get to mm s-1, then multiply by seconds in period to get mm
        [("mm d-1", "mm")] = t => $"-divc,{86400 / t}",
        // W -> j: multiply by timestep. j -> W: divide by timestep.
        // Mj -> W: inverse of (multiply by timestep, divide by 1e6) -> divide by (timestep/1e6)
        [("Mj/m2", "W m-2")] = t => $"-divc,{t / 1e6}",
        [("j/m2", "W m-2")] = t => $"-divc,{t}",
    };

    public record ConversionResult(
        bool RequiresConversion,
        bool RequiresRenaming,
        ConversionDefinition? ConversionExpression = null
    );

    public static ConversionResult AnalyseConversion(string inputUnits, string targetUnits)
    {
        // Check if units are exactly the same (including notation).
        if (inputUnits == targetUnits)
            return new ConversionResult(false, false);

        // Normalise both units to their canonical form
        string normalisedInput = NormaliseUnits(inputUnits);
        string normalisedTarget = NormaliseUnits(targetUnits);

        // Check if units are equivalent (different notation but same meaning).
        if (AreUnitsEquivalent(normalisedInput, normalisedTarget))
            return new ConversionResult(false, true);

        // Check if we have a conversion expression.
        var conversionKey = (normalisedInput, normalisedTarget);
        if (ConversionExpressions.TryGetValue(conversionKey, out var conversion))
            return new ConversionResult(true, true, conversion);

        IEnumerable<string> inputSynonyms = UnitSynonyms.Where(s => s.Value.Contains(normalisedInput)).Select(s => s.Key);
        IEnumerable<string> targetSynonyms = UnitSynonyms.Where(s => s.Value.Contains(normalisedTarget)).Select(s => s.Key);

        foreach (string inputSynonym in inputSynonyms)
        {
            foreach (string targetSynonym in targetSynonyms)
            {
                var synonymConversionKey = (inputSynonym, targetSynonym);
                if (ConversionExpressions.TryGetValue(synonymConversionKey, out var synonymConversion))
                    return new ConversionResult(true, true, synonymConversion);
            }
        }

        throw new ArgumentException($"Unsupported unit conversion from {inputUnits} to {targetUnits}");
    }

    /// <summary>
    /// Normalise a unit string to a canonical form.
    /// </summary>
    /// <param name="units">The unit string to normalise.</param>
    /// <returns>The normalised unit string.</returns>
    internal static string NormaliseUnits(string units)
    {
        // TODO: support spaces:  umol / m2 == umol/m2
        // TODO: support periods: kg.m-2 == kg m-2
        // TODO: support carets:  m2/m2 == m^2/m^2
        // TODO: support slashes: W/m2 == W m-2

        // If no exact match, return as is.
        return units;
    }

    /// <summary>
    /// Checks if two unit strings are equivalent (different notation but same meaning).
    /// </summary>
    /// <param name="units1">The first unit string.</param>
    /// <param name="units2">The second unit string.</param>
    /// <returns>True if the unit strings are equivalent, false otherwise.</returns>
    internal static bool AreUnitsEquivalent(string units1, string units2)
    {
        // First check if they're the same after normalisation.
        if (units1 == units2)
            return true;

        // Then check if they belong to the same synonym group.
        foreach (var synonyms in UnitSynonyms.Values)
            if (synonyms.Contains(units1) && synonyms.Contains(units2))
                return true;

        return false;
    }

    /// <summary>
    /// Generates a CDO operator which performs a unit conversion.
    /// </summary>
    /// <param name="inputUnits">The units of the input variable.</param>
    /// <param name="targetUnits">The units of the output variable.</param>
    /// <param name="timeStep">The time step of the variable.</param>
    /// <returns>A CDO operator which performs the unit conversion.</returns>
    public static string GenerateConversionExpression(
        string inputUnits,
        string targetUnits,
        TimeStep timeStep)
    {
        ConversionResult result = AnalyseConversion(inputUnits, targetUnits);

        // Should never happen - this function is only called if a conversion is
        // required.
        if (!result.RequiresConversion)
            return string.Empty;

        // Conversion expression cannot be null here. AnalyseConversion can only
        // return RequiresConversion=true when the conversion expression is
        // non-null.
        return result.ConversionExpression!(timeStep.GetSecondsInPeriod());
    }
}

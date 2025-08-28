using ClimateProcessing.Models;
using ClimateProcessing.Units;

namespace ClimateProcessing.Services;

public interface IClimateVariableManager
{
    /// <summary>
    /// Gets the output requirements for the specified variable.
    /// </summary>
    /// <param name="variable">The variable.</param>
    /// <returns>The required name and units of the variable.</returns>
    /// <exception cref="ArgumentException">If no configuration is found for the specified variable.</exception>
    VariableInfo GetOutputRequirements(ClimateVariable variable);

    /// <summary>
    /// Gets the set of variables required by the configured model version.
    /// </summary>
    /// <returns>Collection of ClimateVariable values.</returns>
    IEnumerable<ClimateVariable> GetRequiredVariables();

    /// <summary>
    /// Get the aggregation method required for the processing of the specified variable.
    /// </summary>
    /// <param name="variable">The variable.</param>
    /// <returns>The aggregation method.</returns>
    /// <exception cref="ArgumentException">If no configuration is found for the specified variable.</exception>
    AggregationMethod GetAggregationMethod(ClimateVariable variable);

    /// <summary>
    /// Get the metadata of the specified variable required by the model.
    /// </summary>
    /// <param name="variable">The variable.</param>
    /// <returns>The metadata of the variable.</returns>
    /// <exception cref="ArgumentException">If no configuration is found for the specified variable.</exception>
    VariableMetadata GetMetadata(ClimateVariable variable);
}

using ClimateProcessing.Models;

namespace ClimateProcessing.Services;

public interface IRemappingService
{
    /// <summary>
    /// Check if a variable is expressed on a per-ground-area basis.
    /// </summary>
    /// <param name="units">The units of the variable.</param>
    /// <returns>True iff the variable is expressed on a per-ground-area basis.</returns>
    /// <remarks>This is used to decide whether to perform conservative remapping.</remarks>
    bool HasPerAreaUnits(string units);

    /// <summary>
    /// Get an interpolation algorithm to be used when remapping the specified
    /// variable.
    /// </summary>
    /// <param name="info">Metadata for the variable in the dataset being processed.</param>
    /// <param name="variable">The variable to remap.</param>
    /// <returns>The interpolation algorithm to use.</returns>
    InterpolationAlgorithm GetInterpolationAlgorithm(VariableInfo info, ClimateVariable variable);
}

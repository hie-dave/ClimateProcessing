namespace ClimateProcessing.Models;

/// <summary>
/// The format of a climate variable.
/// </summary>
/// <param name="Variable">The variable.</param>
/// <param name="Stage">The stage of processing that the variable is at.</param>
public record ClimateVariableFormat(ClimateVariable Variable, ProcessingStage Stage)
{
    /// <summary>
    /// Creates a <see cref="ClimateVariableFormat"/> representing a variable in
    /// its preprocessed format.
    /// </summary>
    /// <param name="variable">The variable.</param>
    /// <returns>A <see cref="ClimateVariableFormat"/> representing the variable in its preprocessed format.</returns>
    public static ClimateVariableFormat Preprocessed(ClimateVariable variable) => new(variable, ProcessingStage.Preprocessed);

    /// <summary>
    /// Creates a <see cref="ClimateVariableFormat"/> representing a variable in
    /// its timeseries format.
    /// </summary>
    /// <param name="variable">The variable.</param>
    /// <returns>A <see cref="ClimateVariableFormat"/> representing the variable in its timeseries format.</returns>
    public static ClimateVariableFormat Timeseries(ClimateVariable variable) => new(variable, ProcessingStage.Timeseries);

    /// <summary>
    /// Creates a <see cref="ClimateVariableFormat"/> representing a variable in its rechunked format.
    /// </summary>
    /// <param name="variable">The variable.</param>
    /// <returns>A <see cref="ClimateVariableFormat"/> representing the variable in its rechunked format.</returns>
    public static ClimateVariableFormat Rechunked(ClimateVariable variable) => new(variable, ProcessingStage.Rechunked);

    /// <inheritdoc/>
    public override string ToString() => $"{Variable}: ({Stage})";
}

namespace ClimateProcessing.Models;

/// <summary>
/// Information about a variable.
/// </summary>
/// <param name="Name">Name of the variable.</param>
/// <param name="Units">Units of the variable.</param>
public record VariableInfo(string Name, string Units);

namespace ClimateProcessing.Models;

/// <summary>
/// Metadata about a variable.
/// </summary>
/// <param name="StandardName">The standard name of the variable.</param>
/// <param name="LongName">The long name of the variable.</param>
public record VariableMetadata(string StandardName, string LongName);

namespace ClimateProcessing.Services;

/// <summary>
/// Interface for services that sort variable processors based on their dependencies.
/// </summary>
public interface IVariableProcessorSorter
{
    /// <summary>
    /// Sorts the given variable processors in dependency order.
    /// </summary>
    /// <param name="processors">The processors to sort.</param>
    /// <returns>The processors sorted in dependency order.</returns>
    IEnumerable<IVariableProcessor> SortByDependencies(IEnumerable<IVariableProcessor> processors);
}

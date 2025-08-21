using ClimateProcessing.Models;

namespace ClimateProcessing.Services;

/// <summary>
/// Service for sorting variable processors based on their dependencies.
/// </summary>
public class VariableProcessorSorter : IVariableProcessorSorter
{
    /// <summary>
    /// Sorts the given variable processors in dependency order.
    /// </summary>
    /// <param name="processors">The processors to sort.</param>
    /// <returns>The processors sorted in dependency order.</returns>
    public IEnumerable<IVariableProcessor> SortByDependencies(IEnumerable<IVariableProcessor> processors)
    {
        // Create a dictionary mapping from variable format to the processor that produces it
        Dictionary<ClimateVariableFormat, IVariableProcessor> formatToProcessor = new();
        foreach (IVariableProcessor processor in processors)
        {
            IEnumerable<ClimateVariableFormat> formats = [..processor.IntermediateOutputs, processor.OutputFormat];
            foreach (ClimateVariableFormat format in formats)
            {
                if (formatToProcessor.ContainsKey(format))
                    throw new InvalidOperationException($"Multiple processors produce {format}");
                formatToProcessor[format] = processor;
            }
        }

        // Create a dictionary mapping from processor to its dependencies
        Dictionary<IVariableProcessor, HashSet<IVariableProcessor>> processorDependencies = new();
        foreach (IVariableProcessor processor in processors)
        {
            HashSet<IVariableProcessor> dependencies = new();
            foreach (ClimateVariableFormat dependency in processor.Dependencies)
            {
                if (!formatToProcessor.TryGetValue(dependency, out IVariableProcessor? dependencyProcessor))
                    throw new InvalidOperationException($"Processor {processor.OutputFormat} depends on {dependency}, which is not produced by any processor");
                dependencies.Add(dependencyProcessor);
            }
            processorDependencies[processor] = dependencies;
        }

        // Perform topological sort
        List<IVariableProcessor> result = new();
        HashSet<IVariableProcessor> visited = new();
        HashSet<IVariableProcessor> visiting = new();

        foreach (IVariableProcessor processor in processors)
            if (!visited.Contains(processor))
                VisitProcessor(processor, processorDependencies, visited, visiting, result);

        return result;
    }

    /// <summary>
    /// Recursively visits the given processor and its dependencies.
    /// </summary>
    /// <param name="processor">The processor to visit.</param>
    /// <param name="processorDependencies">The dictionary mapping processors to their dependencies.</param>
    /// <param name="visited">The set of processors that have already been visited.</param>
    /// <param name="visiting">The set of processors that are currently being visited.</param>
    /// <param name="result">The list of processors in topological order.</param>
    private void VisitProcessor(
        IVariableProcessor processor,
        Dictionary<IVariableProcessor, HashSet<IVariableProcessor>> processorDependencies,
        HashSet<IVariableProcessor> visited,
        HashSet<IVariableProcessor> visiting,
        List<IVariableProcessor> result)
    {
        if (visiting.Contains(processor))
            throw new InvalidOperationException("Circular dependency detected in variable processors");

        if (visited.Contains(processor))
            return;

        visiting.Add(processor);

        // If the processor has unresolved dependencies, an exception would have
        // been thrown before reaching this point. Therefore, the only reason
        // this processor would not be in the dictionary is that it has no
        // dependencies.
        if (processorDependencies.TryGetValue(processor, out HashSet<IVariableProcessor>? dependencies))
            foreach (IVariableProcessor dependency in dependencies)
                VisitProcessor(dependency, processorDependencies, visited, visiting, result);

        visiting.Remove(processor);
        visited.Add(processor);
        result.Add(processor);
    }
}

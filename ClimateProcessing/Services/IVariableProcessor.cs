using ClimateProcessing.Models;

namespace ClimateProcessing.Services;

/// <summary>
/// Represents a processor for a climate variable.
/// </summary>
public interface IVariableProcessor
{
    /// <summary>
    /// The climate variable this processor produces.
    /// </summary>
    ClimateVariable TargetVariable { get; }

    /// <summary>
    /// The variable formats this processor depends on.
    /// </summary>
    IReadOnlySet<ClimateVariableFormat> Dependencies { get; }

    /// <summary>
    /// Creates the jobs required to process this variable.
    /// </summary>
    /// <param name="dataset">The climate dataset being processed.</param>
    /// <param name="context">Context for creating jobs.</param>
    /// <returns>A collection of jobs that process this variable.</returns>
    Task<IReadOnlyList<Job>> CreateJobsAsync(
        IClimateDataset dataset,
        IJobCreationContext context);
}

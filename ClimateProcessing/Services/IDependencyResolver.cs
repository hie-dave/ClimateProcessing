using ClimateProcessing.Models;

namespace ClimateProcessing.Services;

/// <summary>
/// Resolves dependencies for a job.
/// </summary>
public interface IDependencyResolver
{
    /// <summary>
    /// Returns the job that produces the specified dependency.
    /// </summary>
    /// <param name="dependency">The dependency to resolve.</param>
    /// <returns>The job that produces the dependency.</returns>
    Job GetJob(ClimateVariableFormat dependency);
}

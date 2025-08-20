using ClimateProcessing.Models;

namespace ClimateProcessing.Services;

/// <summary>
/// Resolves dependencies for a job.
/// </summary>
public class DependencyResolver : IDependencyResolver
{
    private readonly List<Job> jobs;

    /// <summary>
    /// Creates a new dependency resolver.
    /// </summary>
    public DependencyResolver() : this([]) { }

    /// <summary>
    /// Creates a new dependency resolver.
    /// </summary>
    /// <param name="jobs">The jobs to resolve dependencies for.</param>
    public DependencyResolver(IEnumerable<Job> jobs)
    {
        this.jobs = jobs.ToList();
    }

    /// <summary>
    /// Adds jobs to the resolver.
    /// </summary>
    /// <param name="jobs">The jobs to add.</param>
    public void AddJobs(IEnumerable<Job> jobs)
    {
        this.jobs.AddRange(jobs);
    }

    /// <summary>
    /// Get all registered jobs.
    /// </summary>
    /// <returns>The jobs.</returns>
    public IEnumerable<Job> GetJobs() => jobs;

    /// <inheritdoc/>
    public Job GetJob(ClimateVariableFormat dependency)
    {
        Job? job = jobs.FirstOrDefault(j => j.Output == dependency);
        if (job == null)
            throw new ArgumentException($"No job found for dependency: {dependency}");
        return job;
    }
}

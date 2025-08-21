using ClimateProcessing.Models;
using ClimateProcessing.Services;
using Xunit;

namespace ClimateProcessing.Tests.Services;

public class DependencyResolverTests
{
    private readonly DependencyResolver resolver = new DependencyResolver();

    [Fact]
    public void Constructor_WithNoJobs_CreatesEmptyResolver()
    {
        DependencyResolver resolver = new DependencyResolver();
        Assert.Empty(resolver.GetJobs());
    }

    [Fact]
    public void Constructor_WithJobs_AddsJobsToResolver()
    {
        Job job1 = CreateJob("job1", ClimateVariable.Temperature);
        Job job2 = CreateJob("job2", ClimateVariable.Precipitation);

        DependencyResolver resolver = new DependencyResolver([job1, job2]);

        Assert.Equal(2, resolver.GetJobs().Count());
        Assert.Contains(job1, resolver.GetJobs());
        Assert.Contains(job2, resolver.GetJobs());
    }

    [Fact]
    public void AddJobs_WithMultipleJobs_AddsAllJobs()
    {
        Job job1 = CreateJob("job1", ClimateVariable.Temperature);
        Job job2 = CreateJob("job2", ClimateVariable.Precipitation);

        resolver.AddJobs([job1, job2]);

        Assert.Equal(2, resolver.GetJobs().Count());
        Assert.Contains(job1, resolver.GetJobs());
        Assert.Contains(job2, resolver.GetJobs());
    }

    [Fact]
    public void AddJobs_CalledMultipleTimes_AccumulatesJobs()
    {
        Job job1 = CreateJob("job1", ClimateVariable.Temperature);
        Job job2 = CreateJob("job2", ClimateVariable.Precipitation);
        Job job3 = CreateJob("job3", ClimateVariable.WindSpeed);

        resolver.AddJobs([job1]);
        resolver.AddJobs([job2, job3]);

        Assert.Equal(3, resolver.GetJobs().Count());
        Assert.Contains(job1, resolver.GetJobs());
        Assert.Contains(job2, resolver.GetJobs());
        Assert.Contains(job3, resolver.GetJobs());
    }

    [Fact]
    public void GetJobs_ReturnsAllAddedJobs()
    {
        Job job1 = CreateJob("job1", ClimateVariable.Temperature);
        Job job2 = CreateJob("job2", ClimateVariable.Precipitation);
        Job job3 = CreateJob("job3", ClimateVariable.WindSpeed);
        resolver.AddJobs([job1, job2, job3]);

        IEnumerable<Job> jobs = resolver.GetJobs();

        Assert.Equal(3, jobs.Count());
        Assert.Contains(job1, jobs);
        Assert.Contains(job2, jobs);
        Assert.Contains(job3, jobs);
    }

    [Fact]
    public void GetJob_WithExistingDependency_ReturnsCorrectJob()
    {
        ClimateVariableFormat format1 = ClimateVariableFormat.Timeseries(ClimateVariable.Temperature);
        ClimateVariableFormat format2 = ClimateVariableFormat.Rechunked(ClimateVariable.Precipitation);
        Job job1 = CreateJob("job1", format1);
        Job job2 = CreateJob("job2", format2);
        resolver.AddJobs([job1, job2]);

        Job result = resolver.GetJob(format2);

        Assert.Equal(job2, result);
    }

    [Fact]
    public void GetJob_WithNonExistentDependency_ThrowsArgumentException()
    {
        ClimateVariableFormat format1 = ClimateVariableFormat.Timeseries(ClimateVariable.Temperature);
        ClimateVariableFormat format2 = ClimateVariableFormat.Rechunked(ClimateVariable.Precipitation);
        Job job1 = CreateJob("job1", format1);
        resolver.AddJobs([job1]);

        ArgumentException exception = Assert.Throws<ArgumentException>(() => resolver.GetJob(format2));
        Assert.Contains($"No job found for dependency: {format2}", exception.Message);
    }

    [Fact]
    public void GetJob_WithMultipleJobsForSameDependency_ReturnsFirstMatch()
    {
        // Same output format for both jobs. This is probably unsupported
        // behaviour, as multiple jobs producing the same output is the
        // definition of an invalid processing pipeline.
        ClimateVariableFormat format = ClimateVariableFormat.Timeseries(ClimateVariable.Temperature);
        Job job1 = CreateJob("job1", format);
        Job job2 = CreateJob("job2", format);
        resolver.AddJobs([job1, job2]);

        Job result = resolver.GetJob(format);

        // For now, let's just ensure we get the first job added.
        // TODO: should this throw?
        Assert.Equal(job1, result);
    }

    [Fact]
    public void GetJob_WithDifferentStages_ReturnsCorrectJob()
    {
        ClimateVariableFormat timeseriesFormat = ClimateVariableFormat.Timeseries(ClimateVariable.Temperature);
        ClimateVariableFormat rechunkedFormat = ClimateVariableFormat.Rechunked(ClimateVariable.Temperature);
        Job job1 = CreateJob("job1", timeseriesFormat);
        Job job2 = CreateJob("job2", rechunkedFormat);
        resolver.AddJobs([job1, job2]);

        Job result1 = resolver.GetJob(timeseriesFormat);
        Job result2 = resolver.GetJob(rechunkedFormat);

        Assert.Equal(job1, result1);
        Assert.Equal(job2, result2);
    }

    /// <summary>
    /// Helper method to create a job which produces the specified output
    /// variable at the timeseries processing stage.
    /// </summary>
    /// <param name="name">The name of the job.</param>
    /// <param name="variable">The variable to process.</param>
    /// <returns>The created job.</returns>
    private static Job CreateJob(string name, ClimateVariable variable)
    {
        return CreateJob(name, ClimateVariableFormat.Timeseries(variable));
    }

    /// <summary>
    /// Helper method to create a job with a specific output format.
    /// </summary>
    /// <param name="name">The name of the job.</param>
    /// <param name="format">The output format of the job.</param>
    /// <returns>The created job.</returns>
    private static Job CreateJob(string name, ClimateVariableFormat format)
    {
        return new Job(
            name,
            $"{name}.sh",
            format,
            $"{name}.nc",
            [] // No dependencies
        );
    }
}

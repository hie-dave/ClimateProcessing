namespace ClimateProcessing.Models;

/// <summary>
/// Represents a job within the processing pipeline.
/// </summary>
public class Job
{
    /// <summary>
    /// The name of the job.
    /// </summary>
    public string Name { get; private init; }

    /// <summary>
    /// The path to the script that will be executed.
    /// </summary>
    public string ScriptPath { get; private init; }

    /// <summary>
    /// The variable and stage of processing that the output will be in.
    /// </summary>
    public ClimateVariableFormat Output { get; private init; }

    /// <summary>
    /// The path to the output file.
    /// </summary>
    public string OutputPath { get; private init; }

    /// <summary>
    /// The jobs that must be completed before this job can begin.
    /// </summary>
    public IReadOnlyList<Job> Dependencies { get; private init; }

    /// <summary>
    /// Creates a new job.
    /// </summary>
    /// <param name="name">The name of the job.</param>
    /// <param name="scriptPath">The path to the script that will be executed.</param>
    /// <param name="output">The variable and stage of processing that the output will be in.</param>
    /// <param name="outputPath">The path to the output file.</param>
    /// <param name="dependencies">The jobs that must be completed before this job can begin.</param>
    public Job(
        string name,
        string scriptPath,
        ClimateVariableFormat output,
        string outputPath,
        IEnumerable<Job> dependencies)
    {
        Name = name;
        ScriptPath = scriptPath;
        Output = output;
        OutputPath = outputPath;
        Dependencies = dependencies.ToList();
    }
}

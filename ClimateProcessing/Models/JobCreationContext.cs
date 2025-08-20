using ClimateProcessing.Services;

namespace ClimateProcessing.Models;

/// <summary>
/// Context for creating a job.
/// </summary>
public class JobCreationContext : IJobCreationContext
{
    /// <inheritdoc />
    public ProcessingConfig Config { get; }

    /// <inheritdoc />
    public IPathManager PathManager { get; }

    /// <inheritdoc />
    public IFileWriterFactory FileWriterFactory { get; }

    /// <inheritdoc />
    public IClimateVariableManager VariableManager { get; }

    /// <inheritdoc />
    public PBSWriter PBSLightweight { get; }

    /// <inheritdoc />
    public PBSWriter PBSHeavyweight { get; }

    /// <inheritdoc />
    public IRemappingService Remapper { get; }

    /// <inheritdoc />
    public IDependencyResolver DependencyResolver { get; }

    /// <summary>
    /// Creates a new job creation context.
    /// </summary>
    /// <param name="processingConfig">The processing configuration.</param>
    /// <param name="pathManager">The path manager.</param>
    /// <param name="fileWriterFactory">The file writer factory.</param>
    public JobCreationContext(
        ProcessingConfig processingConfig,
        IPathManager pathManager,
        IFileWriterFactory fileWriterFactory,
        IClimateVariableManager variableManager,
        PBSWriter pbsLightweight,
        PBSWriter pbsHeavyweight,
        IRemappingService remapper,
        IDependencyResolver dependencyResolver)
    {
        Config = processingConfig;
        PathManager = pathManager;
        FileWriterFactory = fileWriterFactory;
        VariableManager = variableManager;
        PBSLightweight = pbsLightweight;
        PBSHeavyweight = pbsHeavyweight;
        Remapper = remapper;
        DependencyResolver = dependencyResolver;
    }
}

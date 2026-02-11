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
    public IScriptHeaderWriter PBSLightweight { get; }

    /// <inheritdoc />
    public IScriptHeaderWriter PBSHeavyweight { get; }

    /// <inheritdoc />
    public IScriptHeaderWriter PBSPreprocessing { get; }

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
    /// <param name="variableManager">The climate variable manager.</param>
    /// <param name="pbsLightweight">The PBS lightweight script writer.</param>
    /// <param name="pbsHeavyweight">The PBS heavyweight script writer.</param>
    /// <param name="pbsPreprocessing">The PBS preprocessing script writer.</param>
    /// <param name="remapper">The remapping service.</param>
    /// <param name="dependencyResolver">The dependency resolver.</param>
    public JobCreationContext(
        ProcessingConfig processingConfig,
        IPathManager pathManager,
        IFileWriterFactory fileWriterFactory,
        IClimateVariableManager variableManager,
        IScriptHeaderWriter pbsLightweight,
        IScriptHeaderWriter pbsHeavyweight,
        IScriptHeaderWriter pbsPreprocessing,
        IRemappingService remapper,
        IDependencyResolver dependencyResolver)
    {
        Config = processingConfig;
        PathManager = pathManager;
        FileWriterFactory = fileWriterFactory;
        VariableManager = variableManager;
        PBSLightweight = pbsLightweight;
        PBSHeavyweight = pbsHeavyweight;
        PBSPreprocessing = pbsPreprocessing;
        Remapper = remapper;
        DependencyResolver = dependencyResolver;
    }
}

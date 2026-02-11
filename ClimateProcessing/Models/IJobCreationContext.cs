using ClimateProcessing.Services;

namespace ClimateProcessing.Models;

/// <summary>
/// Interface to a class providing context for job creation.
/// </summary>
public interface IJobCreationContext
{
    /// <summary>
    /// The processing configuration.
    /// </summary>
    ProcessingConfig Config { get; }

    /// <summary>
    /// The path manager.
    /// </summary>
    IPathManager PathManager { get; }

    /// <summary>
    /// The file writer factory.
    /// </summary>
    IFileWriterFactory FileWriterFactory { get; }

    /// <summary>
    /// The climate variable manager.
    /// </summary>
    IClimateVariableManager VariableManager { get; }

    /// <summary>
    /// The PBS lightweight script writer.
    /// </summary>
    IScriptHeaderWriter PBSLightweight { get; }

    /// <summary>
    /// The PBS script writer for preprocessing jobs.
    /// </summary>
    IScriptHeaderWriter PBSPreprocessing { get; }

    /// <summary>
    /// The PBS heavyweight script writer.
    /// </summary>
    IScriptHeaderWriter PBSHeavyweight { get; }

    /// <summary>
    /// The remapping service.
    /// </summary>
    IRemappingService Remapper { get; }

    /// <summary>
    /// The dependency resolver.
    /// </summary>
    IDependencyResolver DependencyResolver { get; }
}

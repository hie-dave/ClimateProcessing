using ClimateProcessing.Models;
using ClimateProcessing.Services;
using ClimateProcessing.Tests.Helpers;
using Moq;

namespace ClimateProcessing.Tests.Mocks;

public class TestContext : IJobCreationContext
{
    private readonly InMemoryScriptWriterFactory scriptWriterFactory;
    private readonly Mock<IDependencyResolver> mockDependencyResolver;

    public ProcessingConfig Config { get; set; }

    public IPathManager PathManager { get; set; }

    public IFileWriterFactory FileWriterFactory => scriptWriterFactory;

    public IClimateVariableManager VariableManager { get; set; }

    public IScriptHeaderWriter PBSLightweight { get; set; }

    public IScriptHeaderWriter PBSHeavyweight { get; set; }

    public IRemappingService Remapper { get; set; }

    public IDependencyResolver DependencyResolver => mockDependencyResolver.Object;

    public TestContext()
    {
        scriptWriterFactory = new InMemoryScriptWriterFactory();

        ModelVersion version = ModelVersion.Trunk;

        Config = new TestProcessingConfig();
        Config.Version = version;
        PathManager = new MockPathManager();
        VariableManager = new ClimateVariableManager(version);
        Remapper = new RemappingService(); // TODO: mock?
        mockDependencyResolver = new Mock<IDependencyResolver>();

        // Setup a mock header writer which just writes a simple shebang.
        Mock<IScriptHeaderWriter> mockHeaderWriter = new Mock<IScriptHeaderWriter>();
        mockHeaderWriter.Setup(w => w.WriteHeaderAsync(
            It.IsAny<IFileWriter>(),
            It.IsAny<string>(),
            It.IsAny<IEnumerable<PBSStorageDirective>>())).Callback(async (
                IFileWriter writer,
                string name,
                IEnumerable<PBSStorageDirective> directives) =>
                {
                    await writer.WriteLineAsync($"#!/bin/bash");
                });
        PBSLightweight = mockHeaderWriter.Object;
        PBSHeavyweight = mockHeaderWriter.Object;

    }

    public string ReadScript(Job job)
    {
        return scriptWriterFactory.Read(job.Name);
    }

    public void ConfigureDependency(ClimateVariableFormat format, Func<Job> job)
    {
        mockDependencyResolver.Setup(dr => dr.GetJob(format)).Returns(job);
    }

    public void ConfigureDependency(ClimateVariableFormat format, Job job)
    {
        mockDependencyResolver.Setup(dr => dr.GetJob(format)).Returns(job);
    }

    /// <summary>
    /// Register a randomly-generated job with the dependency resolver.
    /// </summary>
    /// <param name="format">The format of the dependency.</param>
    public void ConfigureDependency(ClimateVariableFormat format)
    {
        Job job = new Job(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), format, Guid.NewGuid().ToString(), []);
        ConfigureDependency(format, job);
    }
}

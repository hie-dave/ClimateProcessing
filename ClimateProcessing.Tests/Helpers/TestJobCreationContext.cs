// using ClimateProcessing.Configuration;
// using ClimateProcessing.Models;
// using ClimateProcessing.Services;
// using ClimateProcessing.Tests.Mocks;

// namespace ClimateProcessing.Tests.Helpers;

// public class TestJobCreationContext : IJobCreationContext
// {
//     internal TestProcessingConfig MutableConfig { get; set; }

//     public ProcessingConfig Config => MutableConfig;

//     public IPathManager PathManager { get; set; }

//     public IFileWriterFactory FileWriterFactory { get; set; }

//     public IClimateVariableManager VariableManager { get; set; }

//     public IScriptHeaderWriter PBSLightweight { get; set; }

//     public IScriptHeaderWriter PBSHeavyweight { get; set; }

//     public IRemappingService Remapper { get; set; }

//     public IDependencyResolver DependencyResolver { get; set; }

//     public TestJobCreationContext(ModelVersion version, string outputDirectory)
//     {
//         MutableConfig = new TestProcessingConfig();
//         MutableConfig.Version = version;
//         MutableConfig.InputTimeStepHours = 24;
//         MutableConfig.OutputTimeStepHours = 24;

//         PBSConfig config = new PBSConfig("q", 1, 1, 1, "", PBSWalltime.Parse("01:00:00"), EmailNotificationType.None, "");

//         PathManager = new PathManager(outputDirectory);
//         FileWriterFactory = new InMemoryScriptWriterFactory();
//         VariableManager = new ClimateVariableManager(version);
//         PBSLightweight = new PBSWriter(config, PathManager);
//         PBSHeavyweight = new PBSWriter(config, PathManager);
//         Remapper = new RemappingService();
//         DependencyResolver = new DependencyResolver();
//     }
// }

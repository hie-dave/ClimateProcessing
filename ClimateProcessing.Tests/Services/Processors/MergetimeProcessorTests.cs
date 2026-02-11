using ClimateProcessing.Configuration;
using ClimateProcessing.Models;
using ClimateProcessing.Models.Options;
using ClimateProcessing.Services;
using ClimateProcessing.Services.Processors;
using ClimateProcessing.Tests.Helpers;
using ClimateProcessing.Tests.Mocks;
using ClimateProcessing.Units;
using Moq;
using System.IO;
using Xunit;

namespace ClimateProcessing.Tests.Services.Processors;

public sealed class MergetimeProcessorTests : IDisposable
{
    private readonly ClimateVariable targetVariable = ClimateVariable.Temperature;
    private readonly TempDirectory workingDirectory;

    public MergetimeProcessorTests()
    {
        workingDirectory = TempDirectory.Create();
    }

    public void Dispose()
    {
        workingDirectory.Dispose();
    }

    [Fact]
    public void Constructor_WithTargetVariable_InitializesProperties()
    {
        MergetimeProcessor processor = new(targetVariable);

        Assert.Equal(targetVariable, processor.TargetVariable);
        Assert.Equal(ClimateVariableFormat.Timeseries(targetVariable), processor.OutputFormat);
        Assert.Empty(processor.IntermediateOutputs);
        Assert.Empty(processor.Dependencies);
    }

    [Fact]
    public void Constructor_WithTargetVariableAndScriptGenerator_InitializesProperties()
    {
        var mockScriptGenerator = new Mock<IMergetimeScriptGenerator>();
        var mockPreprocessor = new Mock<IPreprocessingScriptGenerator>();

        var processor = new MergetimeProcessor(targetVariable, mockPreprocessor.Object, mockScriptGenerator.Object);

        Assert.Equal(targetVariable, processor.TargetVariable);
        Assert.Equal(ClimateVariableFormat.Timeseries(targetVariable), processor.OutputFormat);
        Assert.Empty(processor.IntermediateOutputs);
        Assert.Empty(processor.Dependencies);
    }

    [Fact]
    public async Task CreateJobsAsync_ReturnsExpectedJob()
    {
        var mockScriptGenerator = new Mock<IMergetimeScriptGenerator>();
        var mockPreprocessor = new Mock<IPreprocessingScriptGenerator>();
        var processor = new MergetimeProcessor(targetVariable, mockPreprocessor.Object, mockScriptGenerator.Object);

        string datasetPath = Path.Combine(workingDirectory.AbsolutePath, "dataset");
        string inputDir = Path.Combine(datasetPath, "input");
        const string outputDirectory = "o";
        const string outputFileName = "tas_timeseries.nc";
        const string datasetName = "TestDataset";
        string outDir = Path.Combine(workingDirectory.AbsolutePath, "tmp", outputDirectory);
        string outputFile = Path.Combine(outDir, datasetName, outputFileName);

        // Mock dataset
        var mockDataset = new Mock<IClimateDataset>();
        mockDataset.Setup(d => d.DatasetName).Returns(datasetName);
        mockDataset.Setup(d => d.GetOutputDirectory()).Returns(outputDirectory);
        mockDataset.Setup(d => d.GetInputFilesDirectory(targetVariable)).Returns(inputDir);
        mockDataset.Setup(d => d.GenerateOutputFileName(targetVariable, It.IsAny<VariableInfo>())).Returns(outputFileName);

        VariableInfo variableInfo = new("tas", "K");
        mockDataset.Setup(d => d.GetVariableInfo(targetVariable)).Returns(variableInfo);

        TestJobCreationContext context = new TestJobCreationContext(ModelVersion.Trunk, workingDirectory.AbsolutePath);
        context.MockPathManager.SetOutputFileName(targetVariable, outputFileName);
        context.MockPathManager.SetBasePath(PathType.Working, outDir);

        // Setup script generator to verify options
        mockPreprocessor.Setup(sg => sg.WritePreprocessingScriptAsync(It.IsAny<IFileWriter>(), It.IsAny<IPreprocessingOptions>()))
            .Callback<IFileWriter, IPreprocessingOptions>((writer, options) => 
            {
                // Verify options
                Assert.Equal(inputDir, options.InputDirectory);
                Assert.Equal(variableInfo, options.InputMetadata);
                Assert.Equal(variableInfo, options.TargetMetadata);
                Assert.Equal(context.Config.InputTimeStep, options.InputTimeStep);
                Assert.Equal(context.Config.OutputTimeStep, options.OutputTimeStep);
                Assert.Equal(AggregationMethod.Mean, options.AggregationMethod);
                Assert.Null(options.GridFile);
                Assert.Equal(InterpolationAlgorithm.Bilinear, options.RemapAlgorithm);
                Assert.True(options.Unpack);
                // Assert.False(options.Compress);
                Assert.Same(mockDataset.Object, options.Dataset);
            })
            .Returns(Task.CompletedTask);
        string? mergetimeInputPath = null;
        mockScriptGenerator.Setup(sg => sg.WriteMergetimeScriptAsync(It.IsAny<IFileWriter>(), It.IsAny<IMergetimeOptions>()))
            .Callback<IFileWriter, IMergetimeOptions>((writer, options) => 
            {
                // Verify options
                Assert.Equal(Path.GetFullPath(outputFile), options.OutputFile);
                mergetimeInputPath = options.InputDirectory;
            })
            .Returns(Task.CompletedTask);

        var jobs = await processor.CreateJobsAsync(mockDataset.Object, context);

        // This now creates two jobs - preprocessing, and mergetime.
        Assert.Equal(2, jobs.Count);
        var preprocessingJob = jobs[0];
        Assert.Equal($"preprocessing_{variableInfo.Name}_{datasetName}", preprocessingJob.Name);
        Assert.Equal(InMemoryScriptWriter.ScriptName, preprocessingJob.ScriptPath);
        Assert.Equal(ClimateVariableFormat.Preprocessed(targetVariable), preprocessingJob.Output);
        Assert.Empty(preprocessingJob.Dependencies);

        var mergetimeJob = jobs[1];

        Assert.Equal($"mergetime_{variableInfo.Name}_{mockDataset.Object.DatasetName}", mergetimeJob.Name);
        Assert.Equal(InMemoryScriptWriter.ScriptName, mergetimeJob.ScriptPath);
        Assert.Equal(ClimateVariableFormat.Timeseries(targetVariable), mergetimeJob.Output);
        Assert.Equal(Path.GetFullPath(outputFile), mergetimeJob.OutputPath);
        Job dep = Assert.Single(mergetimeJob.Dependencies);
        Assert.Same(preprocessingJob, dep);

        // Verify mergetime input path is the output of the preprocessing job.
        Assert.Equal(preprocessingJob.OutputPath, mergetimeInputPath);

        // Verify script generator was called
        mockScriptGenerator.Verify(sg => sg.WriteMergetimeScriptAsync(It.IsAny<IFileWriter>(), It.IsAny<IMergetimeOptions>()), Times.Once);
    }

    [Fact]
    public async Task CreateJobsAsync_WithGridFile_PassesGridFileToOptions()
    {
        var mockScriptGenerator = new Mock<IMergetimeScriptGenerator>();
        var mockPreprocessingGenerator = new Mock<IPreprocessingScriptGenerator>();
        var processor = new MergetimeProcessor(targetVariable, mockPreprocessingGenerator.Object, mockScriptGenerator.Object);

        string inputPath = Path.Combine(workingDirectory.AbsolutePath, "input");
        string outputPath = Path.Combine(workingDirectory.AbsolutePath, "output");
        DynamicMockDataset dataset = new(inputPath, outputPath);
        

        // Mock config with grid file
        string gridFile = "/path/to/grid.txt";
        var context = new TestJobCreationContext(ModelVersion.Trunk, outputPath);
        context.MutableConfig.GridFile = gridFile;

        bool gridFileVerified = false;
        mockScriptGenerator.Setup(sg => sg.WriteMergetimeScriptAsync(It.IsAny<IFileWriter>(), It.IsAny<IMergetimeOptions>()))
            .Returns(Task.CompletedTask);
        mockPreprocessingGenerator.Setup(pg => pg.WritePreprocessingScriptAsync(It.IsAny<IFileWriter>(), It.IsAny<IPreprocessingOptions>()))
            .Callback<IFileWriter, IPreprocessingOptions>((writer, options) => 
            {
                Assert.Equal(gridFile, options.GridFile);
                gridFileVerified = true;
            })
            .Returns(Task.CompletedTask);

        await processor.CreateJobsAsync(dataset, context);

        Assert.True(gridFileVerified, "Grid file was not properly passed to options");
    }

    [Fact]
    public async Task CreateJobsAsync_WithDifferentTimeSteps_PassesTimeStepsToOptions()
    {
        var mockScriptGenerator = new Mock<IMergetimeScriptGenerator>();
        var mockPreprocessingGenerator = new Mock<IPreprocessingScriptGenerator>();
        var processor = new MergetimeProcessor(targetVariable, mockPreprocessingGenerator.Object, mockScriptGenerator.Object);

        string inputPath = Path.Combine(workingDirectory.AbsolutePath, "in");
        string outputPath = Path.Combine(workingDirectory.AbsolutePath, "out");
        DynamicMockDataset dataset = new(inputPath, outputPath);

        TestJobCreationContext context = new TestJobCreationContext(ModelVersion.Trunk, outputPath);
        context.MutableConfig.InputTimeStepHours = 3;
        context.MutableConfig.OutputTimeStepHours = 24;

        bool timeStepsVerified = false;
        mockScriptGenerator.Setup(sg => sg.WriteMergetimeScriptAsync(It.IsAny<IFileWriter>(), It.IsAny<IMergetimeOptions>()))
            .Returns(Task.CompletedTask);
        mockPreprocessingGenerator.Setup(pg => pg.WritePreprocessingScriptAsync(It.IsAny<IFileWriter>(), It.IsAny<IPreprocessingOptions>()))
            .Callback<IFileWriter, IPreprocessingOptions>((writer, options) =>
            {
                Assert.Equal(3, options.InputTimeStep.Hours);
                Assert.Equal(24, options.OutputTimeStep.Hours);
                timeStepsVerified = true;
            })
            .Returns(Task.CompletedTask);
        await processor.CreateJobsAsync(dataset, context);

        Assert.True(timeStepsVerified, "Time steps were not properly passed to options");
    }

    [Fact]
    public async Task CreateJobsAsync_WithDifferentVariableNames_PassesCorrectMetadataToOptions()
    {
        Mock<IMergetimeScriptGenerator> mockScriptGenerator = new();
        Mock<IPreprocessingScriptGenerator> mockPreprocessingScriptGenerator = new();
        MergetimeProcessor processor = new(targetVariable, mockPreprocessingScriptGenerator.Object, mockScriptGenerator.Object);

        DynamicMockDataset dataset = new DynamicMockDataset(workingDirectory.AbsolutePath);
        dataset.SetVariableInfo(targetVariable, "temperature", "C");

        VariableInfo outputVariableInfo = new("tas", "K");
        Mock<IClimateVariableManager> mockVariableManager = new();
        mockVariableManager.Setup(vm => vm.GetOutputRequirements(targetVariable)).Returns(outputVariableInfo);

        TestJobCreationContext context = new(ModelVersion.Trunk, workingDirectory.AbsolutePath);
        context.VariableManager = mockVariableManager.Object;

        bool metadataVerified = false;
        mockScriptGenerator.Setup(sg => sg.WriteMergetimeScriptAsync(It.IsAny<IFileWriter>(), It.IsAny<IMergetimeOptions>()))
            .Callback<IFileWriter, IMergetimeOptions>((writer, options) => 
            {
            })
            .Returns(Task.CompletedTask);
        mockPreprocessingScriptGenerator.Setup(pg => pg.WritePreprocessingScriptAsync(It.IsAny<IFileWriter>(), It.IsAny<IPreprocessingOptions>()))
            .Callback<IFileWriter, IPreprocessingOptions>((writer, options) =>
            {
                Assert.Equal(dataset.GetVariableInfo(targetVariable), options.InputMetadata);
                Assert.Equal(outputVariableInfo, options.TargetMetadata);
                metadataVerified = true;
            })
            .Returns(Task.CompletedTask);

        await processor.CreateJobsAsync(dataset, context);

        Assert.True(metadataVerified, "Variable metadata was not properly passed to options");
    }
}

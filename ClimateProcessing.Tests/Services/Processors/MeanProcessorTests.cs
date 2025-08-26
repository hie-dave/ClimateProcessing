using ClimateProcessing.Configuration;
using ClimateProcessing.Models;
using ClimateProcessing.Services;
using ClimateProcessing.Services.Processors;
using ClimateProcessing.Tests.Helpers;
using Moq;
using Xunit;

namespace ClimateProcessing.Tests.Services.Processors;

public class MeanProcessorTests
{
    private const string testOutputFileName = "test_output.nc";
    private readonly ClimateVariable targetVariable = ClimateVariable.Temperature;
    private readonly List<ClimateVariable> dependencies = [ClimateVariable.MinTemperature, ClimateVariable.MaxTemperature];

    [Fact]
    public void Constructor_WithLessThanTwoDependencies_ThrowsArgumentException()
    {
        List<ClimateVariable> singleDependency = [ClimateVariable.MinTemperature];

        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            new MeanProcessor(testOutputFileName, targetVariable, singleDependency));

        Assert.Equal("dependencies", exception.ParamName);
        Assert.Contains("requires at least two dependencies", exception.Message);
    }

    [Fact]
    public void Constructor_WithValidParameters_InitializesProperties()
    {
        MeanProcessor processor = new MeanProcessor(testOutputFileName, targetVariable, dependencies);

        Assert.Equal(targetVariable, processor.TargetVariable);
        Assert.Equal(ClimateVariableFormat.Timeseries(targetVariable), processor.OutputFormat);
        Assert.Empty(processor.IntermediateOutputs);

        HashSet<ClimateVariableFormat> expectedDependencies = [.. dependencies.Select(ClimateVariableFormat.Timeseries)];
        Assert.Equal(expectedDependencies, processor.Dependencies);
    }

    [Fact]
    public async Task CreateJobsAsync_ReturnsExpectedJob()
    {
        MeanProcessor processor = new MeanProcessor(testOutputFileName, targetVariable, dependencies);

        string workingDir = "/working/dir";
        string datasetPath = $"{workingDir}/dataset";
        string outputPath = $"{datasetPath}/{testOutputFileName}";

        List<string> inputFiles = [
            $"{workingDir}/min_temp.nc",
            $"{workingDir}/max_temp.nc"
        ];

        // Mock dataset
        Mock<IClimateDataset> mockDataset = new Mock<IClimateDataset>();
        mockDataset.Setup(d => d.DatasetName).Returns("TestDataset");

        // Mock path manager
        Mock<IPathManager> mockPathManager = new Mock<IPathManager>();
        mockPathManager.Setup(pm => pm.GetDatasetFileName(mockDataset.Object, dependencies[0], PathType.Working, It.IsAny<IClimateVariableManager>()))
            .Returns(inputFiles[0]);
        mockPathManager.Setup(pm => pm.GetDatasetFileName(mockDataset.Object, dependencies[1], PathType.Working, It.IsAny<IClimateVariableManager>()))
            .Returns(inputFiles[1]);
        mockPathManager.Setup(pm => pm.GetDatasetPath(mockDataset.Object, PathType.Working))
            .Returns(datasetPath);
        mockPathManager.Setup(pm => pm.GetBasePath(PathType.Log))
            .Returns("/path/to/logs");
        mockPathManager.Setup(pm => pm.GetBasePath(PathType.Stream))
            .Returns("/path/to/streams");

        // Use InMemoryScriptWriterFactory
        InMemoryScriptWriterFactory fileWriterFactory = new InMemoryScriptWriterFactory();

        // Create PBS config and real PBS writer
        PBSWriter pbsWriter = CreatePBSWriter(mockPathManager.Object);
        // Mock variable manager
        Mock<IClimateVariableManager> mockVariableManager = new Mock<IClimateVariableManager>();

        VariableInfo minTemp = new VariableInfo("tasmin", "K");
        VariableInfo maxTemp = new VariableInfo("tasmax", "K");
        VariableInfo temp = new VariableInfo("tas", "K");

        mockVariableManager.Setup(vm => vm.GetOutputRequirements(ClimateVariable.MinTemperature)).Returns(minTemp);
        mockVariableManager.Setup(vm => vm.GetOutputRequirements(ClimateVariable.MaxTemperature)).Returns(maxTemp);
        mockVariableManager.Setup(vm => vm.GetOutputRequirements(ClimateVariable.Temperature)).Returns(temp);

        // Mock dependency resolver
        Mock<IDependencyResolver> mockDependencyResolver = new Mock<IDependencyResolver>();

        Job minTempJob = new Job("min_temp", "/scripts/min_temp.sh",
            ClimateVariableFormat.Timeseries(ClimateVariable.MinTemperature),
            inputFiles[0], []);

        Job maxTempJob = new Job("max_temp", "/scripts/max_temp.sh",
            ClimateVariableFormat.Timeseries(ClimateVariable.MaxTemperature),
            inputFiles[1], []);

        mockDependencyResolver.Setup(dr => dr.GetJob(ClimateVariableFormat.Timeseries(ClimateVariable.MinTemperature)))
            .Returns(minTempJob);
        mockDependencyResolver.Setup(dr => dr.GetJob(ClimateVariableFormat.Timeseries(ClimateVariable.MaxTemperature)))
            .Returns(maxTempJob);

        // Mock context
        Mock<IJobCreationContext> mockContext = new Mock<IJobCreationContext>();
        mockContext.Setup(c => c.PathManager).Returns(mockPathManager.Object);
        mockContext.Setup(c => c.FileWriterFactory).Returns(fileWriterFactory);
        mockContext.Setup(c => c.PBSLightweight).Returns(pbsWriter);
        mockContext.Setup(c => c.VariableManager).Returns(mockVariableManager.Object);
        mockContext.Setup(c => c.DependencyResolver).Returns(mockDependencyResolver.Object);

        IReadOnlyList<Job> jobs = await processor.CreateJobsAsync(mockDataset.Object, mockContext.Object);

        Assert.Single(jobs);
        Job job = jobs[0];

        Assert.Equal($"calc_mean_{targetVariable}", job.Name);
        Assert.Equal(InMemoryScriptWriter.ScriptName, job.ScriptPath);
        Assert.Equal(ClimateVariableFormat.Timeseries(targetVariable), job.Output);
        Assert.Equal(outputPath, job.OutputPath);
        Assert.Equal(2, job.Dependencies.Count);
        Assert.Contains(minTempJob, job.Dependencies);
        Assert.Contains(maxTempJob, job.Dependencies);

        // Verify script content using InMemoryScriptWriterFactory
        string scriptContent = fileWriterFactory.Read(job.Name);

        // We could also verify that the equation gets written to $EQN_FILE.
        ValidateScript(scriptContent, job, inputFiles, temp, [minTemp, maxTemp]);
    }

    [Fact]
    public async Task CreateJobsAsync_WithMoreThanTwoDependencies_WritesCorrectEquation()
    {
        List<ClimateVariable> threeDependencies = [
            ClimateVariable.MinTemperature,
            ClimateVariable.MaxTemperature,
            ClimateVariable.Temperature
        ];

        MeanProcessor processor = new MeanProcessor(testOutputFileName, targetVariable, threeDependencies);

        // Use InMemoryScriptWriterFactory
        InMemoryScriptWriterFactory fileWriterFactory = new InMemoryScriptWriterFactory();

        // Mock dataset
        Mock<IClimateDataset> mockDataset = new Mock<IClimateDataset>();

        // Mock path manager
        string inputFileName = "/path/to/file.nc";
        Mock<IPathManager> mockPathManager = new Mock<IPathManager>();
        mockPathManager.Setup(pm => pm.GetDatasetFileName(It.IsAny<IClimateDataset>(), It.IsAny<ClimateVariable>(), PathType.Working, It.IsAny<IClimateVariableManager>()))
            .Returns(inputFileName);
        mockPathManager.Setup(pm => pm.GetDatasetPath(It.IsAny<IClimateDataset>(), PathType.Working))
            .Returns("/path/to");
        mockPathManager.Setup(pm => pm.GetBasePath(PathType.Log))
            .Returns("/path/to/logs");
        mockPathManager.Setup(pm => pm.GetBasePath(PathType.Stream))
            .Returns("/path/to/streams");

        // Create PBS config and real PBS writer.
        PBSWriter pbsWriter = CreatePBSWriter(mockPathManager.Object);

        // Mock variable manager
        Mock<IClimateVariableManager> mockVariableManager = new Mock<IClimateVariableManager>();
        mockVariableManager.Setup(vm => vm.GetOutputRequirements(ClimateVariable.MinTemperature))
            .Returns(new VariableInfo("tasmin", "K"));
        mockVariableManager.Setup(vm => vm.GetOutputRequirements(ClimateVariable.MaxTemperature))
            .Returns(new VariableInfo("tasmax", "K"));
        mockVariableManager.Setup(vm => vm.GetOutputRequirements(ClimateVariable.Temperature))
            .Returns(new VariableInfo("tas", "K"));

        // Mock dependency resolver
        Mock<IDependencyResolver> mockDependencyResolver = new Mock<IDependencyResolver>();
        mockDependencyResolver.Setup(dr => dr.GetJob(It.IsAny<ClimateVariableFormat>()))
            .Returns(new Job("test", "/path/to/script.sh", ClimateVariableFormat.Timeseries(ClimateVariable.Temperature), "/path/to/output.nc", []));

        // Mock context
        Mock<IJobCreationContext> mockContext = new Mock<IJobCreationContext>();
        mockContext.Setup(c => c.PathManager).Returns(mockPathManager.Object);
        mockContext.Setup(c => c.FileWriterFactory).Returns(fileWriterFactory);
        mockContext.Setup(c => c.PBSLightweight).Returns(pbsWriter);
        mockContext.Setup(c => c.VariableManager).Returns(mockVariableManager.Object);
        mockContext.Setup(c => c.DependencyResolver).Returns(mockDependencyResolver.Object);

        IReadOnlyList<Job> jobs = await processor.CreateJobsAsync(mockDataset.Object, mockContext.Object);

        Job job = Assert.Single(jobs);
        string scriptContent = fileWriterFactory.Read(job.Name);
        ValidateScript(
            scriptContent,
            job,
            [inputFileName, inputFileName, inputFileName],
            mockVariableManager.Object.GetOutputRequirements(targetVariable),
            threeDependencies.Select(mockVariableManager.Object.GetOutputRequirements).ToList());
    }

    private static PBSWriter CreatePBSWriter(IPathManager pathManager)
    {
        PBSConfig pbsConfig = new PBSConfig(
            "normal",
            1,
            4,
            10,
            "test_project",
            PBSWalltime.Parse("01:00:00"),
            EmailNotificationType.None,
            null);

        return new PBSWriter(pbsConfig, pathManager);
    }

    private static void ValidateScript(string scriptContent, Job job, List<string> inputFiles, VariableInfo output, List<VariableInfo> inputs)
    {
        Assert.Contains($"#PBS -N {job.Name}", scriptContent);
        Assert.Contains($"IN_FILES=\"{string.Join(" ", inputFiles)}\"", scriptContent);
        Assert.Contains($"OUT_FILE=\"{job.OutputPath}\"", scriptContent);
        Assert.Contains($"{output.Name}=({string.Join(" + ", inputs.Select(i => i.Name))})/{inputs.Count}", scriptContent);
        Assert.Contains("EOF", scriptContent);
        Assert.Contains("exprf,\"${EQN_FILE}\" -merge ${IN_FILES} \"${OUT_FILE}\"", scriptContent);
    }
}

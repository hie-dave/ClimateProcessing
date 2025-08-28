using ClimateProcessing.Models;
using ClimateProcessing.Services;
using Moq;
using Xunit;

namespace ClimateProcessing.Tests.Services;

public class PathManagerTests : IDisposable
{
    private readonly TempDirectory outputPath;
    private readonly TempDirectory dataPath;
    private readonly PathManager pathManager;
    private readonly Mock<IClimateDataset> mockDataset;

    public PathManagerTests()
    {
        outputPath = TempDirectory.Create();
        dataPath = TempDirectory.Create();

        // Setup path manager.
        pathManager = new PathManager(outputPath.AbsolutePath);

        // Setup some dependencies.
        mockDataset = new Mock<IClimateDataset>();
        mockDataset.Setup(d => d.GetOutputDirectory()).Returns(dataPath.AbsolutePath);
    }

    public void Dispose()
    {
        outputPath.Dispose();
        dataPath.Dispose();
    }

    [Theory]
    [InlineData(PathType.Log)]
    [InlineData(PathType.Script)]
    [InlineData(PathType.Stream)]
    public void GetDatasetPath_ThrowsForInvalidPathTypes(PathType pathType)
    {
        var ex = Assert.Throws<ArgumentException>(() => pathManager.GetDatasetPath(mockDataset.Object, pathType));
        Assert.Contains("not valid at the dataset-level", ex.Message);
    }

    [Theory]
    [InlineData(PathType.Output, "output")]
    [InlineData(PathType.Working, "tmp")]
    public void GetDatasetPath_ReturnsCorrectPathForValidTypes(PathType pathType, string expectedBaseDir)
    {
        string result = pathManager.GetDatasetPath(mockDataset.Object, pathType);

        string expectedPath = Path.Combine(outputPath.AbsolutePath, expectedBaseDir, dataPath.AbsolutePath);
        Assert.Equal(expectedPath, result);
        Assert.True(Directory.Exists(result));
    }

    [Fact]
    public void GetDatasetFileName_ReturnsCorrectPath()
    {
        const string fileName = "test_file.nc";
        mockDataset.Setup(d => d.GenerateOutputFileName(It.IsAny<ClimateVariable>(), It.IsAny<VariableInfo>()))
            .Returns(fileName);
        mockDataset.Setup(d => d.GetVariableInfo(It.IsAny<ClimateVariable>()))
            .Returns(new VariableInfo(fileName, "units"));
        Mock<IClimateVariableManager> mockVariableManager = new Mock<IClimateVariableManager>();
        mockVariableManager.Setup(v => v.GetOutputRequirements(It.IsAny<ClimateVariable>()))
            .Returns(new VariableInfo(fileName, "units"));

        string result = pathManager.GetDatasetFileName(mockDataset.Object, ClimateVariable.Precipitation, PathType.Output, mockVariableManager.Object);

        string expectedPath = Path.Combine(outputPath.AbsolutePath, "output", dataPath.AbsolutePath, fileName);
        Assert.Equal(expectedPath, result);
        Assert.True(Directory.Exists(Path.GetDirectoryName(result)));
    }

    [Fact]
    public void GetDatasetFileName_RenamesVariableIfNecessary()
    {
        const string oldName = "prAdjust";
        const string newName = "pr";

        mockDataset.Setup(d => d.GetVariableInfo(ClimateVariable.Precipitation))
            .Returns(new VariableInfo(oldName, "units"));
        mockDataset.Setup(d => d.GenerateOutputFileName(ClimateVariable.Precipitation, It.IsAny<VariableInfo>()))
            .Returns($"{oldName}.nc");

        Mock<IClimateVariableManager> mockVariableManager = new Mock<IClimateVariableManager>();
        mockVariableManager.Setup(v => v.GetOutputRequirements(ClimateVariable.Precipitation))
            .Returns(new VariableInfo(newName, "units"));

        string result = pathManager.GetDatasetFileName(mockDataset.Object, ClimateVariable.Precipitation, PathType.Output, mockVariableManager.Object);
        string fileName = Path.GetFileName(result);

        Assert.DoesNotContain(oldName, result);
        Assert.Contains(newName, result); // TODO: startsWith?
    }

    [Fact]
    public void GetChecksumFilePath_ReturnsCorrectPath()
    {
        string result = pathManager.GetChecksumFilePath();

        string expectedPath = Path.Combine(outputPath.AbsolutePath, "output", "sha512sums.txt");
        Assert.Equal(expectedPath, result);
    }

    [Fact]
    public void CreateDirectoryTree_CreatesAllRequiredDirectories()
    {
        const string datasetDir = "test_dataset";
        mockDataset.Setup(d => d.GetOutputDirectory()).Returns(datasetDir);

        pathManager.CreateDirectoryTree(mockDataset.Object);

        // Check top-level directories
        Assert.True(Directory.Exists(Path.Combine(outputPath.AbsolutePath, "logs")));
        Assert.True(Directory.Exists(Path.Combine(outputPath.AbsolutePath, "scripts")));
        Assert.True(Directory.Exists(Path.Combine(outputPath.AbsolutePath, "streams")));
        Assert.True(Directory.Exists(Path.Combine(outputPath.AbsolutePath, "output")));
        Assert.True(Directory.Exists(Path.Combine(outputPath.AbsolutePath, "tmp")));

        // Check dataset-specific directories
        Assert.True(Directory.Exists(Path.Combine(outputPath.AbsolutePath, "output", dataPath.AbsolutePath)));
        Assert.True(Directory.Exists(Path.Combine(outputPath.AbsolutePath, "tmp", dataPath.AbsolutePath)));
    }

    [Fact]
    public void GetBasePath_ThrowsForInvalidPathType()
    {
        Assert.Throws<ArgumentException>(() => pathManager.GetBasePath((PathType)123));
    }
}

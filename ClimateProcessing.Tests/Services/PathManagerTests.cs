using ClimateProcessing.Models;
using ClimateProcessing.Services;
using Moq;
using Xunit;

namespace ClimateProcessing.Tests.Services;

public class PathManagerTests : IDisposable
{
    private readonly string outputPath;
    private readonly TempDirectory tempDirectory;

    private readonly PathManager pathManager;
    private readonly Mock<IClimateDataset> mockDataset;

    public PathManagerTests()
    {
        tempDirectory = TempDirectory.Create();
        outputPath = tempDirectory.AbsolutePath;

        pathManager = new PathManager(outputPath);
        mockDataset = new Mock<IClimateDataset>();
    }

    public void Dispose()
    {
        tempDirectory.Dispose();
    }

    [Theory]
    [InlineData(PathType.Log)]
    [InlineData(PathType.Script)]
    [InlineData(PathType.Stream)]
    public void GetDatasetPath_ThrowsForInvalidPathTypes(PathType pathType)
    {
        mockDataset.Setup(d => d.GetOutputDirectory()).Returns("test_dataset");
        var ex = Assert.Throws<ArgumentException>(() => pathManager.GetDatasetPath(mockDataset.Object, pathType));
        Assert.Contains("not valid at the dataset-level", ex.Message);
    }

    [Theory]
    [InlineData(PathType.Output)]
    [InlineData(PathType.Working)]
    public void GetDatasetPath_ReturnsCorrectPathForValidTypes(PathType pathType)
    {
        const string datasetDir = "test_dataset";
        mockDataset.Setup(d => d.GetOutputDirectory()).Returns(datasetDir);

        string result = pathManager.GetDatasetPath(mockDataset.Object, pathType);

        string expectedBaseDir = pathType == PathType.Output ? "output" : "tmp";
        string expectedPath = Path.Combine(outputPath, expectedBaseDir, datasetDir);
        Assert.Equal(expectedPath, result);
        Assert.True(Directory.Exists(result));
    }

    [Fact]
    public void GetDatasetFileName_ReturnsCorrectPath()
    {
        const string datasetDir = "test_dataset";
        const string fileName = "test_file.nc";
        mockDataset.Setup(d => d.GetOutputDirectory()).Returns(datasetDir);
        mockDataset.Setup(d => d.GenerateOutputFileName(It.IsAny<ClimateVariable>()))
            .Returns(fileName);

        string result = pathManager.GetDatasetFileName(mockDataset.Object, ClimateVariable.Precipitation, PathType.Output);

        string expectedPath = Path.Combine(outputPath, "output", datasetDir, fileName);
        Assert.Equal(expectedPath, result);
        Assert.True(Directory.Exists(Path.GetDirectoryName(result)));
    }

    [Fact]
    public void GetChecksumFilePath_ReturnsCorrectPath()
    {
        string result = pathManager.GetChecksumFilePath();

        string expectedPath = Path.Combine(outputPath, "output", "sha512sums.txt");
        Assert.Equal(expectedPath, result);
    }

    [Fact]
    public void CreateDirectoryTree_CreatesAllRequiredDirectories()
    {
        const string datasetDir = "test_dataset";
        mockDataset.Setup(d => d.GetOutputDirectory()).Returns(datasetDir);

        pathManager.CreateDirectoryTree(mockDataset.Object);

        // Check top-level directories
        Assert.True(Directory.Exists(Path.Combine(outputPath, "logs")));
        Assert.True(Directory.Exists(Path.Combine(outputPath, "scripts")));
        Assert.True(Directory.Exists(Path.Combine(outputPath, "streams")));
        Assert.True(Directory.Exists(Path.Combine(outputPath, "output")));
        Assert.True(Directory.Exists(Path.Combine(outputPath, "tmp")));

        // Check dataset-specific directories
        Assert.True(Directory.Exists(Path.Combine(outputPath, "output", datasetDir)));
        Assert.True(Directory.Exists(Path.Combine(outputPath, "tmp", datasetDir)));
    }

    [Fact]
    public void GetBasePath_ThrowsForInvalidPathType()
    {
        Assert.Throws<ArgumentException>(() => pathManager.GetBasePath((PathType)123));
    }
}

using ClimateProcessing.Models;
using ClimateProcessing.Tests.Mocks;
using Xunit;

namespace ClimateProcessing.Tests.Models;

public class ProcessingConfigTests : IDisposable
{
    private readonly TestProcessingConfig config;
    private readonly TempDirectory tempDirectory;

    public ProcessingConfigTests()
    {
        tempDirectory = TempDirectory.Create(GetType().Name);
        config = new TestProcessingConfig
        {
            InputDirectory = tempDirectory.AbsolutePath,
            Project = "TestProject",
            Memory = 1000,
            ChunkSizeTime = 10,
            ChunkSizeSpatial = 5,
            CompressionLevel = 5,
            Version = ModelVersion.Trunk
        };
    }

    public void Dispose()
    {
        tempDirectory.Dispose();
    }

    [Fact]
    public void ValidateDirectories_WithNonExistentInputDirectory_ThrowsArgumentException()
    {
        config.InputDirectory = Path.Combine(tempDirectory.AbsolutePath, "nonexistent");
        var ex = Assert.Throws<ArgumentException>(() => config.ValidateDirectories());
        Assert.Contains("Input directory does not exist", ex.Message);
    }

    [Fact]
    public void ValidateDirectories_WithNonExistentGridFile_ThrowsArgumentException()
    {
        config.GridFile = Path.Combine(tempDirectory.AbsolutePath, "nonexistent.grid");
        var ex = Assert.Throws<ArgumentException>(() => config.ValidateDirectories());
        Assert.Contains("Grid file does not exist", ex.Message);
    }

    [Fact]
    public void ValidateBasicParameters_WithEmptyProject_ThrowsArgumentException()
    {
        config.Project = "";
        var ex = Assert.Throws<ArgumentException>(() => config.ValidateBasicParameters());
        Assert.Equal("Project must be specified", ex.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    public void ValidateBasicParameters_WithInvalidCompressionLevel_ThrowsArgumentException(int level)
    {
        config.CompressOutput = true;
        config.CompressionLevel = level;
        var ex = Assert.Throws<ArgumentException>(config.ValidateBasicParameters);
        Assert.Equal("Compression level must be between 1 and 9", ex.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    public void ValidateBasicParameters_WithInvalidCompressionLevel_CompressionDisabled_DoesNotThrow(int level)
    {
        config.CompressOutput = false;
        config.CompressionLevel = level;
        var exception = Record.Exception(config.ValidateBasicParameters);
        Assert.Null(exception);
    }

    [Fact]
    public void ValidateBasicParameters_WithNegativeMemory_ThrowsArgumentException()
    {
        config.Memory = -1;
        var ex = Assert.Throws<ArgumentException>(() => config.ValidateBasicParameters());
        Assert.Equal("Memory must be greater than 0", ex.Message);
    }

    [Fact]
    public void ValidateBasicParameters_WithInvalidChunkSizeTime_ThrowsArgumentException()
    {
        config.ChunkSizeTime = 0;
        var ex = Assert.Throws<ArgumentException>(() => config.ValidateBasicParameters());
        Assert.Equal("Chunk size must be greater than 0", ex.Message);
    }

    [Fact]
    public void ValidateBasicParameters_WithInvalidChunkSizeSpatial_ThrowsArgumentException()
    {
        config.ChunkSizeSpatial = 0;
        var ex = Assert.Throws<ArgumentException>(() => config.ValidateBasicParameters());
        Assert.Equal("Chunk size for spatial variables must be greater than 0", ex.Message);
    }

    [Theory]
    [InlineData(12)]
    [InlineData(1)]
    public void ValidateTrunkTimeStepSettings_WithNonDailyInputTimeStep_ThrowsArgumentException(int hours)
    {
        config.Version = ModelVersion.Trunk;
        config.InputTimeStepHours = hours;
        var ex = Assert.Throws<ArgumentException>(() => config.ValidateTrunkTimeStepSettings());
        Assert.Contains("Input timestep must be daily (24 hours)", ex.Message);
    }

    [Theory]
    [InlineData(12)]
    [InlineData(8)]
    public void ValidateTrunkTimeStepSettings_WithNonDailyOutputTimeStep_ThrowsArgumentException(int hours)
    {
        config.Version = ModelVersion.Trunk;
        config.OutputTimeStepHours = hours;
        var ex = Assert.Throws<ArgumentException>(() => config.ValidateTrunkTimeStepSettings());
        Assert.Contains("Output timestep must be daily (24 hours)", ex.Message);
    }

    [Fact]
    public void ValidateDaveTimeStepSettings_WithDailyOutputTimeStep_ThrowsArgumentException()
    {
        config.Version = ModelVersion.Dave;
        config.InputTimeStepHours = 24;
        config.OutputTimeStepHours = 24;
        // Exception should not be thrown.
        config.ValidateDaveTimeStepSettings();
    }

    [Fact]
    public void ValidateDaveTimeStepSettings_WithUnspecifiedInputTimeStep_ThrowsArgumentException()
    {
        config.Version = ModelVersion.Dave;
        config.InputTimeStepHours = 0;
        config.OutputTimeStepHours = 3;
        var ex = Assert.Throws<ArgumentException>(() => config.ValidateDaveTimeStepSettings());
        Assert.Equal("Input timestep must be specified when processing for Dave.", ex.Message);
    }

    [Fact]
    public void ValidateDaveTimeStepSettings_WithUnspecifiedOutputTimeStep_ThrowsArgumentException()
    {
        config.Version = ModelVersion.Dave;
        config.OutputTimeStepHours = 0;
        config.InputTimeStepHours = 3;
        var ex = Assert.Throws<ArgumentException>(() => config.ValidateDaveTimeStepSettings());
        Assert.Equal("Output timestep must be specified when processing for Dave.", ex.Message);
    }

    [Fact]
    public void ValidateTimeStepSettings_WithCoarserInputThanOutput_ThrowsArgumentException()
    {
        config.Version = ModelVersion.Dave;
        config.InputTimeStepHours = 6;
        config.OutputTimeStepHours = 12;
        config.ValidateTimeStepSettings();  // Should not throw

        config.InputTimeStepHours = 12;
        config.OutputTimeStepHours = 6;
        var ex = Assert.Throws<ArgumentException>(() => config.ValidateTimeStepSettings());
        Assert.Equal("Input timestep cannot be coarser than the output timestep.", ex.Message);
    }

    [Fact]
    public void ValidateTimeStepSettings_InvalidVersion_ThrowsArgumentException()
    {
        config.Version = (ModelVersion)16;
        ArgumentException ex = Assert.Throws<ArgumentException>(config.ValidateTimeStepSettings);
        Assert.Equal("Invalid version: 16", ex.Message);
    }

    [Fact]
    public void Validate_WithValidConfig_DoesNotThrow()
    {
        var tempGridFile = Path.Combine(tempDirectory.AbsolutePath, "test.grid");
        File.WriteAllText(tempGridFile, "test");
        
        config.GridFile = tempGridFile;
        config.Version = ModelVersion.Trunk;
        config.InputTimeStepHours = 24;
        config.OutputTimeStepHours = 24;

        var exception = Record.Exception(() => config.Validate());
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(EmailNotificationType.Aborted)]
    [InlineData(EmailNotificationType.After)]
    [InlineData(EmailNotificationType.Before)]
    public void ValidateEmailSettings_InvalidFlagCombination_ThrowsArgumentException(EmailNotificationType type)
    {
        config.EmailNotifications = EmailNotificationType.None | type;
        ArgumentException ex = Assert.Throws<ArgumentException>(config.ValidateEmailSettings);
        Assert.Contains("none", ex.Message);
    }

    [Fact]
    public void ValidateEmailSettings_NoEmail_ThrowsArgumentException()
    {
        config.Email = string.Empty;
        config.EmailNotifications = EmailNotificationType.After;
        ArgumentException ex = Assert.Throws<ArgumentException>(config.ValidateEmailSettings);
        Assert.Contains("email", ex.Message);
    }
}

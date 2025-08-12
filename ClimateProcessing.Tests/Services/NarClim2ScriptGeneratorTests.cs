using Xunit;
using ClimateProcessing.Models;
using Moq;
using ClimateProcessing.Services;
using ClimateProcessing.Tests.Mocks;

using static ClimateProcessing.Tests.Helpers.AssertionHelpers;
using static ClimateProcessing.Tests.Helpers.ResourceHelpers;
using Xunit.Abstractions;

namespace ClimateProcessing.Tests.Services;

public class NarClim2ScriptGeneratorTests : IDisposable
{
    private const string outputDirectoryPrefix = "narclim2_script_generator_tests_output";
    private readonly string outputDirectory;
    private readonly NarClim2Config config;
    private readonly NarClim2ScriptGenerator generator;
    private readonly ITestOutputHelper outputHelper;

    public NarClim2ScriptGeneratorTests(ITestOutputHelper outputHelper)
    {
        this.outputHelper = outputHelper;
        outputDirectory = Path.Combine(Path.GetTempPath(), $"{outputDirectoryPrefix}_{Guid.NewGuid()}");
        Directory.CreateDirectory(outputDirectory);

        config = new NarClim2Config
        {
            OutputDirectory = outputDirectory,
            ChunkSizeSpatial = 10,
            ChunkSizeTime = 5,
            CompressOutput = true,
            CompressionLevel = 4,
            InputTimeStepHours = 3,
            OutputTimeStepHours = 3,
            Version = ModelVersion.Dave
        };

        generator = new NarClim2ScriptGenerator(config);
    }

    public void Dispose()
    {
        if (Directory.Exists(outputDirectory))
            Directory.Delete(outputDirectory, true);
    }

    /// <summary>
    /// Create a NarClim2Dataset which doesn't require the existence of a full
    /// dataset on the filesystem.
    /// </summary>
    /// <param name="basePath">The base path of the dataset.</param>
    /// <param name="domain">The domain of the dataset.</param>
    /// <param name="gcm">The GCM of the dataset.</param>
    /// <param name="experiment">The experiment of the dataset.</param>
    /// <param name="rcm">The RCM of the dataset.</param>
    /// <param name="frequency">The frequency of the dataset.</param>
    /// <param name="outputFileName">The name of the output file returned by GenerateOutputFileName() for any climate variable.</param>
    /// <returns>A mocked narclim2 dataset.</returns>
    private Mock<NarClim2Dataset> CreateMockDataset(
        string basePath = "/mock/path",
        NarClim2Domain domain = NarClim2Domain.AUS18,
        NarClim2GCM gcm = NarClim2GCM.AccessEsm15,
        NarClim2Experiment experiment = NarClim2Experiment.Historical,
        NarClim2RCM rcm = NarClim2RCM.WRF412R3,
        NarClim2Frequency frequency = NarClim2Frequency.Month,
        string outputFileName = "/mock/output/file.nc"
    )
    {
        var mockDataset = new Mock<NarClim2Dataset>(
            basePath,
            domain,
            gcm,
            experiment,
            rcm,
            frequency);
        mockDataset.CallBase = true;
        mockDataset.Setup(x => x.GenerateOutputFileName(It.IsAny<ClimateVariable>()))
            .Returns(outputFileName);
        return mockDataset;
    }

    [Theory]
    [InlineData(NarClim2Domain.AUS18, NarClim2Constants.Files.RlonValuesFileAUS18)]
    [InlineData(NarClim2Domain.SEAus04, NarClim2Constants.Files.RlonValuesFileSEAus04)]
    public async Task GenerateVariableMergeScript_UsesCorrectPath(NarClim2Domain domain, string expectedFileName)
    {
        Mock<NarClim2Dataset> mockDataset = CreateMockDataset(domain: domain);
        string file = await generator.GenerateVariableMergeScript(mockDataset.Object, ClimateVariable.Temperature);
        string script = await File.ReadAllTextAsync(file);

        // The script should use the correct rlon values file for this domain.
        Assert.Contains("setvar.py", script);
        Assert.Contains("--var rlon", script);
        Assert.Contains(expectedFileName, script);
    }

    [Fact]
    public async Task GenerateVariableMergeScript_WithNonNarClim2Dataset_ThrowsArgumentException()
    {
        IClimateDataset mockDataset = new StaticMockDataset("/input");

        await Assert.ThrowsAsync<ArgumentException>(() =>
            generator.GenerateVariableMergeScript(mockDataset, ClimateVariable.ShortwaveRadiation));
    }

    [Fact]
    public async Task GenerateScriptsAsync_WithNoInputFileTree_Throws()
    {
        NarClim2Dataset dataset = new NarClim2Dataset("/path/to/narclim2");

        // Attempting to generate scripts without setting up an appropriate
        // file tree is going to result in an exception.
        // TODO: is it worth setting up a suitable file tree?
        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await generator.GenerateScriptsAsync(dataset));

        Assert.NotNull(ex);
        Assert.Contains("No input files found for variable", ex.Message);
    }

    [Fact]
    public async Task NarClim2Generator_IntegrationTest()
    {
        // Create a dummy file tree for a single variable.
        using TempDirectory narclimDirectory = TempDirectory.Create("GenerateScriptsAsync_WithDummyFileTree_GeneratesValidScript");
        string relPath = "CMIP6/DD/AUS-18/NSW-Government/ACCESS-ESM1-5/historical/r6i1p1f1/NARCliM2-0-WRF412R3/v1-r1/mon/{0}/latest";

        string[] vars = ["tas", "pr", "ps", "rsds", "huss", "sfcWind"];
        Dictionary<string, TempDirectory> dirs = new(
            vars.Select(v => new KeyValuePair<string, TempDirectory>(v,
                TempDirectory.Relative(narclimDirectory, string.Format(relPath, v)))));
        using DisposableEnumerable<TempDirectory> tempDirectories = new(dirs.Values);

        string[] fileNames = [
            "{0}_AUS-18_ACCESS-ESM1-5_historical_r6i1p1f1_NSW-Government_NARCliM2-0-WRF412R3_v1-r1_mon_195101-195112.nc",
            "{0}_AUS-18_ACCESS-ESM1-5_historical_r6i1p1f1_NSW-Government_NARCliM2-0-WRF412R3_v1-r1_mon_195201-195212.nc"
        ];
        using DisposableEnumerable<TempFile> files = new(vars.SelectMany(v =>
            fileNames.Select(name => new TempFile(dirs[v].AbsolutePath, string.Format(name, v)))));

        NarClim2Dataset dataset = new NarClim2Dataset(narclimDirectory.AbsolutePath);
        string script = await generator.GenerateScriptsAsync(dataset);
        Assert.NotNull(script);

        AssertEmptyDirectory(Path.Combine(outputDirectory, "logs"));
        AssertEmptyDirectory(Path.Combine(outputDirectory, "streams"));
        AssertEmptyDirectory(Path.Combine(outputDirectory, "output", dataset.GetOutputDirectory()));
        AssertEmptyDirectory(Path.Combine(outputDirectory, "tmp", dataset.GetOutputDirectory()));

        string scriptsDirectory = Path.Combine(outputDirectory, "scripts");
        Assert.True(Directory.Exists(scriptsDirectory));
        Assert.NotEmpty(Directory.EnumerateFileSystemEntries(scriptsDirectory));

        string[] expectedScriptNames = [
            "calc_vpd_NARCliM2.0_AUS-18_ACCESS-ESM1-5_historical_NARCliM2-0-WRF412R3",
            "cleanup_NARCliM2.0_AUS-18_ACCESS-ESM1-5_historical_NARCliM2-0-WRF412R3",
            "mergetime_huss_NARCliM2.0_AUS-18_ACCESS-ESM1-5_historical_NARCliM2-0-WRF412R3",
            "mergetime_pr_NARCliM2.0_AUS-18_ACCESS-ESM1-5_historical_NARCliM2-0-WRF412R3",
            "mergetime_ps_NARCliM2.0_AUS-18_ACCESS-ESM1-5_historical_NARCliM2-0-WRF412R3",
            "mergetime_rsds_NARCliM2.0_AUS-18_ACCESS-ESM1-5_historical_NARCliM2-0-WRF412R3",
            "mergetime_sfcWind_NARCliM2.0_AUS-18_ACCESS-ESM1-5_historical_NARCliM2-0-WRF412R3",
            "mergetime_tas_NARCliM2.0_AUS-18_ACCESS-ESM1-5_historical_NARCliM2-0-WRF412R3",
            "rechunk_huss_NARCliM2.0_AUS-18_ACCESS-ESM1-5_historical_NARCliM2-0-WRF412R3",
            "rechunk_pr_NARCliM2.0_AUS-18_ACCESS-ESM1-5_historical_NARCliM2-0-WRF412R3",
            "rechunk_ps_NARCliM2.0_AUS-18_ACCESS-ESM1-5_historical_NARCliM2-0-WRF412R3",
            "rechunk_rsds_NARCliM2.0_AUS-18_ACCESS-ESM1-5_historical_NARCliM2-0-WRF412R3",
            "rechunk_sfcWind_NARCliM2.0_AUS-18_ACCESS-ESM1-5_historical_NARCliM2-0-WRF412R3",
            "rechunk_tas_NARCliM2.0_AUS-18_ACCESS-ESM1-5_historical_NARCliM2-0-WRF412R3",
            "rechunk_vpd_NARCliM2.0_AUS-18_ACCESS-ESM1-5_historical_NARCliM2-0-WRF412R3",
            "submit_NARCliM2.0_AUS-18_ACCESS-ESM1-5_historical_NARCliM2-0-WRF412R3"
        ];

        Assert.Equal(expectedScriptNames.Length, Directory.EnumerateFileSystemEntries(scriptsDirectory).Count());

        // Name of the directory containing this test's data files.
        const string resourcePrefix = "NarClim2Generator_IntegrationTest";
        foreach (string scriptName in expectedScriptNames)
        {
            string actualScriptPath = Path.Combine(scriptsDirectory, scriptName);
            Assert.True(File.Exists(actualScriptPath), $"Script {actualScriptPath} does not exist.");
            string actualScript = await File.ReadAllTextAsync(actualScriptPath);

            // Read expected script from resource in assembly.
            string expectedScript = await ReadResourceAsync($"{resourcePrefix}.{scriptName}");
            expectedScript = expectedScript.Replace("@#OUTPUT_DIRECTORY#@", outputDirectory);
            expectedScript = expectedScript.Replace("@#INPUT_DIRECTORY#@", narclimDirectory.AbsolutePath);

            // No custom error messages in xunit, apparently.
            if (expectedScript != actualScript)
                outputHelper.WriteLine($"Script {scriptName} is invalid");

            Assert.Equal(expectedScript, actualScript);
        }
    }
}

using Xunit;
using ClimateProcessing.Models;
using Moq;
using ClimateProcessing.Services;
using ClimateProcessing.Tests.Mocks;

using static ClimateProcessing.Tests.Helpers.AssertionHelpers;
using static ClimateProcessing.Tests.Helpers.ResourceHelpers;
using Xunit.Abstractions;
using ClimateProcessing.Units;
using ClimateProcessing.Tests.Helpers;
using ClimateProcessing.Models.Options;

namespace ClimateProcessing.Tests.Services;

public sealed class NarClim2MergetimeScriptGeneratorTests : IDisposable
{
    private readonly TempDirectory outputDirectory;
    private readonly NarClim2MergetimeScriptGenerator generator;
    private readonly ITestOutputHelper outputHelper;

    public NarClim2MergetimeScriptGeneratorTests(ITestOutputHelper outputHelper)
    {
        this.outputHelper = outputHelper;
        outputDirectory = TempDirectory.Create(GetType().Name);
        generator = new NarClim2MergetimeScriptGenerator();
    }

    public void Dispose()
    {
        outputDirectory.Dispose();
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

    private MergetimeOptions CreateOptions(IClimateDataset dataset)
    {
        return new MergetimeOptions(
            "${IN_DIR}",
            "${OUT_FILE}",
            new VariableInfo("tas", "K"),
            new VariableInfo("tas", "K"),
            TimeStep.Daily,
            TimeStep.Daily,
            AggregationMethod.Mean,
            null,
            InterpolationAlgorithm.Conservative,
            false,
            false,
            dataset);
    }

    [Theory]
    [InlineData(NarClim2Domain.AUS18, NarClim2Constants.Files.RlonValuesFileAUS18)]
    [InlineData(NarClim2Domain.SEAus04, NarClim2Constants.Files.RlonValuesFileSEAus04)]
    public async Task GenerateVariableMergeScript_UsesCorrectPath(NarClim2Domain domain, string expectedFileName)
    {
        Mock<NarClim2Dataset> mockDataset = CreateMockDataset(domain: domain);
        var options = CreateOptions(mockDataset.Object);

        using InMemoryScriptWriter writer = new();
        await generator.WriteMergetimeScriptAsync(writer, options);
        string script = writer.GetContent();

        // The script should use the correct rlon values file for this domain.
        Assert.Contains("setvar.py", script);
        Assert.Contains("--var rlon", script);
        Assert.Contains(expectedFileName, script);
    }

    [Fact]
    public async Task GenerateVariableMergeScript_WithNonNarClim2Dataset_ThrowsArgumentException()
    {
        IClimateDataset mockDataset = new StaticMockDataset("/input");
        var options = CreateOptions(mockDataset);

        using InMemoryScriptWriter writer = new();
        await Assert.ThrowsAsync<ArgumentException>(() =>
            generator.WriteMergetimeScriptAsync(writer, options));
    }

    [Fact]
    public void GetRemapOperator_UnknownAlgorithm_ThrowsArgumentException()
    {
        InterpolationAlgorithm algorithm = (InterpolationAlgorithm)999;
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => CdoMergetimeScriptGenerator.GetRemapOperator(algorithm));
        Assert.Contains("algorithm", exception.Message);
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

        ProcessingConfig config = new NarClim2Config()
        {
            OutputDirectory = outputDirectory.AbsolutePath,
            ChunkSizeSpatial = 10,
            ChunkSizeTime = 5,
            CompressOutput = true,
            CompressionLevel = 4,
            InputTimeStepHours = 3,
            OutputTimeStepHours = 3,
            Version = ModelVersion.Dave
        };
        PathManager pathManager = new(outputDirectory.AbsolutePath);
        FileWriterFactory factory = new(pathManager);

        ScriptOrchestrator generator = new ScriptOrchestrator(config, pathManager, factory, new RemappingService());
        string script = await generator.GenerateScriptsAsync(dataset);
        Assert.NotNull(script);

        AssertEmptyDirectory(Path.Combine(outputDirectory.AbsolutePath, "logs"));
        AssertEmptyDirectory(Path.Combine(outputDirectory.AbsolutePath, "streams"));
        AssertEmptyDirectory(Path.Combine(outputDirectory.AbsolutePath, "output", dataset.GetOutputDirectory()));
        AssertEmptyDirectory(Path.Combine(outputDirectory.AbsolutePath, "tmp", dataset.GetOutputDirectory()));

        string scriptsDirectory = Path.Combine(outputDirectory.AbsolutePath, "scripts");
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
            expectedScript = expectedScript.Replace("@#OUTPUT_DIRECTORY#@", outputDirectory.AbsolutePath);
            expectedScript = expectedScript.Replace("@#INPUT_DIRECTORY#@", narclimDirectory.AbsolutePath);

            // No custom error messages in xunit, apparently.
            if (expectedScript != actualScript)
                outputHelper.WriteLine($"Script {scriptName} is invalid");

            Assert.Equal(expectedScript, actualScript);
        }
    }
}

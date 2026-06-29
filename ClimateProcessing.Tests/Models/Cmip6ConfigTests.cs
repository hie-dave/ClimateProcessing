using ClimateProcessing.Models;
using ClimateProcessing.Models.Cmip6;
using Xunit;

namespace ClimateProcessing.Tests.Models;

public sealed class Cmip6ConfigTests : IDisposable
{
    private readonly TempDirectory tempDirectory;
    private const string ValidProject = "test_project";

    public Cmip6ConfigTests()
    {
        tempDirectory = TempDirectory.Create(GetType().Name);
    }

    public void Dispose()
    {
        tempDirectory.Dispose();
    }

    [Fact]
    public void Constructor_InitializesCmip6Defaults()
    {
        Cmip6Config config = new();

        Assert.Equal(1, config.InputTimeStepHours);
        Assert.Null(config.Gcms);
        Assert.Null(config.Experiments);
    }

    [Fact]
    public void CreateDatasets_WithNoFilters_ReturnsAllCombinations()
    {
        Cmip6Config config = CreateValidConfig();

        List<Cmip6Dataset> datasets = config.CreateDatasets().ToList();

        int expectedCount = Enum.GetValues<Cmip6Gcm>().Length *
                            Enum.GetValues<Cmip6Experiment>().Length;
        Assert.Equal(expectedCount, datasets.Count);

        foreach (Cmip6Gcm gcm in Enum.GetValues<Cmip6Gcm>())
            foreach (Cmip6Experiment experiment in Enum.GetValues<Cmip6Experiment>())
                Assert.Contains(datasets, ds => ds.Gcm == gcm && ds.Experiment == experiment);
    }

    [Fact]
    public void CreateDatasets_WithNullGcms_ReturnsAllGcms()
    {
        Cmip6Config config = CreateValidConfig();
        config.Gcms = null;
        config.Experiments = ["ssp245"];

        List<Cmip6Dataset> datasets = config.CreateDatasets().ToList();

        Assert.Equal(Enum.GetValues<Cmip6Gcm>().Length, datasets.Count);
        foreach (Cmip6Gcm gcm in Enum.GetValues<Cmip6Gcm>())
            Assert.Contains(datasets, ds => ds.Gcm == gcm && ds.Experiment == Cmip6Experiment.Ssp245);
    }

    [Fact]
    public void CreateDatasets_WithEmptyGcms_ReturnsAllGcms()
    {
        Cmip6Config config = CreateValidConfig();
        config.Gcms = [];
        config.Experiments = ["ssp119"];

        List<Cmip6Dataset> datasets = config.CreateDatasets().ToList();

        Assert.Equal(Enum.GetValues<Cmip6Gcm>().Length, datasets.Count);
        Assert.All(datasets, ds => Assert.Equal(Cmip6Experiment.Ssp119, ds.Experiment));
    }

    [Fact]
    public void CreateDatasets_WithNullExperiments_ReturnsAllExperiments()
    {
        Cmip6Config config = CreateValidConfig();
        config.Gcms = ["NorESM2-MM"];
        config.Experiments = null;

        List<Cmip6Dataset> datasets = config.CreateDatasets().ToList();

        Assert.Equal(Enum.GetValues<Cmip6Experiment>().Length, datasets.Count);
        foreach (Cmip6Experiment experiment in Enum.GetValues<Cmip6Experiment>())
            Assert.Contains(datasets, ds => ds.Gcm == Cmip6Gcm.NorEsm2MM && ds.Experiment == experiment);
    }

    [Fact]
    public void CreateDatasets_WithEmptyExperiments_ReturnsAllExperiments()
    {
        Cmip6Config config = CreateValidConfig();
        config.Gcms = ["EC-Earth3"];
        config.Experiments = [];

        List<Cmip6Dataset> datasets = config.CreateDatasets().ToList();

        Assert.Equal(Enum.GetValues<Cmip6Experiment>().Length, datasets.Count);
        Assert.All(datasets, ds => Assert.Equal(Cmip6Gcm.ECEarth3, ds.Gcm));
    }

    [Theory]
    [InlineData(Cmip6Gcm.AccessEsm15)]
    [InlineData(Cmip6Gcm.NorEsm2MM)]
    [InlineData(Cmip6Gcm.ECEarth3)]
    public void CreateDatasets_WithSingleGcm_FiltersCorrectly(Cmip6Gcm gcm)
    {
        Cmip6Config config = CreateValidConfig();
        config.Gcms = [Cmip6GcmExtensions.ToString(gcm)];

        List<Cmip6Dataset> datasets = config.CreateDatasets().ToList();

        Assert.NotEmpty(datasets);
        Assert.All(datasets, ds => Assert.Equal(gcm, ds.Gcm));
        Assert.Equal(Enum.GetValues<Cmip6Experiment>().Length, datasets.Count);
    }

    [Theory]
    [InlineData(Cmip6Experiment.Ssp119)]
    [InlineData(Cmip6Experiment.Ssp245)]
    public void CreateDatasets_WithSingleExperiment_FiltersCorrectly(Cmip6Experiment experiment)
    {
        Cmip6Config config = CreateValidConfig();
        config.Experiments = [Cmip6ExperimentExtensions.ToString(experiment)];

        List<Cmip6Dataset> datasets = config.CreateDatasets().ToList();

        Assert.NotEmpty(datasets);
        Assert.All(datasets, ds => Assert.Equal(experiment, ds.Experiment));
        Assert.Equal(Enum.GetValues<Cmip6Gcm>().Length, datasets.Count);
    }

    [Theory]
    [InlineData(Cmip6Gcm.AccessEsm15, Cmip6Experiment.Ssp119)]
    [InlineData(Cmip6Gcm.NorEsm2MM, Cmip6Experiment.Ssp245)]
    [InlineData(Cmip6Gcm.ECEarth3, Cmip6Experiment.Ssp245)]
    public void CreateDatasets_WithMultipleFilters_AppliesAllFilters(Cmip6Gcm gcm, Cmip6Experiment experiment)
    {
        Cmip6Config config = CreateValidConfig();
        config.Gcms = [Cmip6GcmExtensions.ToString(gcm)];
        config.Experiments = [Cmip6ExperimentExtensions.ToString(experiment)];

        List<Cmip6Dataset> datasets = config.CreateDatasets().ToList();

        Cmip6Dataset dataset = Assert.Single(datasets);
        Assert.Equal(gcm, dataset.Gcm);
        Assert.Equal(experiment, dataset.Experiment);
        Assert.Equal($"{Cmip6GcmExtensions.ToString(gcm)}_{Cmip6ExperimentExtensions.ToString(experiment)}", dataset.DatasetName);
    }

    [Fact]
    public void CreateDatasets_UsesConfiguredInputDirectory()
    {
        Cmip6Config config = CreateValidConfig();
        config.Gcms = ["ACCESS-ESM1-5"];
        config.Experiments = ["ssp245"];

        Cmip6Dataset dataset = Assert.Single(config.CreateDatasets());

        string expected = Path.Combine(tempDirectory.AbsolutePath, "ACCESS-ESM1-5", "ssp245", "pr");
        Assert.Equal(expected, dataset.GetInputFilesDirectory(ClimateVariable.Precipitation));
    }

    [Theory]
    [InlineData("invalid-gcm")]
    [InlineData("ACCESS-ESM1-5 ")]
    [InlineData("access-esm1-5")]
    public void CreateDatasets_WithInvalidGcm_Throws(string invalidGcm)
    {
        Cmip6Config config = CreateValidConfig();
        config.Gcms = [invalidGcm];

        Assert.Throws<ArgumentException>(() => config.CreateDatasets().ToList());
    }

    [Theory]
    [InlineData("invalid-experiment")]
    [InlineData("SSP245")]
    [InlineData("ssp370")]
    public void CreateDatasets_WithInvalidExperiment_Throws(string invalidExperiment)
    {
        Cmip6Config config = CreateValidConfig();
        config.Experiments = [invalidExperiment];

        Assert.Throws<ArgumentException>(() => config.CreateDatasets().ToList());
    }

    [Fact]
    public void CreateDatasets_WithDuplicateGcms_ProducesDuplicateDatasets()
    {
        Cmip6Config config = CreateValidConfig();
        config.Gcms = ["ACCESS-ESM1-5", "ACCESS-ESM1-5"];
        config.Experiments = ["ssp245"];

        List<Cmip6Dataset> datasets = config.CreateDatasets().ToList();

        Assert.Equal(2, datasets.Count);
        Assert.All(datasets, ds => Assert.Equal("ACCESS-ESM1-5_ssp245", ds.DatasetName));
    }

    [Fact]
    public void CreateDatasets_WithDuplicateExperiments_ProducesDuplicateDatasets()
    {
        Cmip6Config config = CreateValidConfig();
        config.Gcms = ["NorESM2-MM"];
        config.Experiments = ["ssp119", "ssp119"];

        List<Cmip6Dataset> datasets = config.CreateDatasets().ToList();

        Assert.Equal(2, datasets.Count);
        Assert.All(datasets, ds => Assert.Equal("NorESM2-MM_ssp119", ds.DatasetName));
    }

    [Fact]
    public void Validate_WithValidDaveConfig_DoesNotThrow()
    {
        Cmip6Config config = CreateValidConfig();

        Exception? exception = Record.Exception(config.Validate);

        Assert.Null(exception);
    }

    [Fact]
    public void Validate_WithInvalidInputDirectory_Throws()
    {
        Cmip6Config config = CreateValidConfig();
        config.InputDirectory = Path.Combine(tempDirectory.AbsolutePath, "does-not-exist");

        Assert.Throws<ArgumentException>(config.Validate);
    }

    [Fact]
    public void Validate_WithTrunkVersion_NormalisesTimestepsToDaily()
    {
        Cmip6Config config = CreateValidConfig();
        config.Version = ModelVersion.Trunk;
        config.InputTimeStepHours = 0;
        config.OutputTimeStepHours = 0;

        config.Validate();

        Assert.Equal(24, config.InputTimeStepHours);
        Assert.Equal(24, config.OutputTimeStepHours);
    }

    private Cmip6Config CreateValidConfig()
    {
        return new Cmip6Config
        {
            InputDirectory = tempDirectory.AbsolutePath,
            Project = ValidProject,
            Version = ModelVersion.Dave,
            OutputTimeStepHours = 24
        };
    }
}

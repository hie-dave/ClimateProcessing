using ClimateProcessing.Configuration;
using ClimateProcessing.Models;
using CommandLine;
using Xunit;

namespace ClimateProcessing.Tests.Models;

public sealed class BarpaConfigTests : IDisposable
{
    private readonly TempDirectory tempDirectory = TempDirectory.Create(nameof(BarpaConfigTests));
    private const string PbsProject = "test_project";

    public void Dispose() => tempDirectory.Dispose();

    // An independent description of regional archive availability. Expected
    // results deliberately do not call the IsValidFor methods used by the config.
    private static readonly IReadOnlyDictionary<string, string[]> regionalExperiments =
        new Dictionary<string, string[]>
        {
            ["ERA5"] = ["evaluation"],
            ["ACCESS-CM2"] = ["historical", "ssp126", "ssp370", "ssp585"],
            ["ACCESS-ESM1-5"] = ["historical", "ssp126", "ssp370"],
            ["NorESM2-MM"] = ["historical", "ssp126", "ssp370"],
            ["EC-Earth3"] = ["historical", "ssp126", "ssp370", "ssp585"],
            ["CESM2"] = ["historical", "ssp126", "ssp370"],
            ["CMCC-ESM2"] = ["historical", "ssp126", "ssp370"],
            ["MPI-ESM1-2-HR"] = ["historical", "ssp126", "ssp370"]
        };

    [Theory]
    [InlineData(1, "1hr", false)]
    [InlineData(1, "1hr", true)]
    [InlineData(3, "3hr", false)]
    [InlineData(3, "3hr", true)]
    [InlineData(6, "6hr", false)]
    [InlineData(6, "6hr", true)]
    [InlineData(24, "day", false)]
    [InlineData(24, "day", true)]
    public void UnspecifiedFilters_ValidateAndCreateAllCompatibleDatasets(
        int hours, string frequency, bool emptyFilters)
    {
        BarpaConfig config = CreateValidConfig(hours);
        if (emptyFilters)
            config.GCMs = config.Domains = config.Projects = config.Experiments = [];

        config.Validate();

        AssertOutputDirectories(ExpectedDefaultDirectories(hours, frequency), config.CreateDatasets());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CreateDatasets_UnspecifiedGcmsSelectAllCompatibleModels(bool emptyFilter)
    {
        BarpaConfig config = CreateFilteredConfig();
        config.GCMs = emptyFilter ? [] : null;
        string[] expected =
        [
            "output-CMIP6/AUS-15/ACCESS-CM2/historical/day",
            "output-CMIP6/AUS-15/ACCESS-ESM1-5/historical/day",
            "output-CMIP6/AUS-15/NorESM2-MM/historical/day",
            "output-CMIP6/AUS-15/EC-Earth3/historical/day",
            "output-CMIP6/AUS-15/CESM2/historical/day",
            "output-CMIP6/AUS-15/CMCC-ESM2/historical/day",
            "output-CMIP6/AUS-15/MPI-ESM1-2-HR/historical/day"
        ];

        config.Validate();

        AssertOutputDirectories(expected, config.CreateDatasets());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CreateDatasets_UnspecifiedDomainsSelectAllCompatibleDomains(bool emptyFilter)
    {
        BarpaConfig config = CreateFilteredConfig();
        config.Domains = emptyFilter ? [] : null;

        config.Validate();

        AssertOutputDirectories(
            ["output-CMIP6/AUS-15/ACCESS-CM2/historical/day", "output-CMIP6/AUST-15/ACCESS-CM2/historical/day"],
            config.CreateDatasets());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CreateDatasets_UnspecifiedProjectsSelectAllCompatibleProjects(bool emptyFilter)
    {
        BarpaConfig config = CreateFilteredConfig();
        config.Projects = emptyFilter ? [] : null;
        config.Domains = ["AUST-04"];
        config.GCMs = ["ERA5"];
        config.Experiments = ["evaluation"];

        config.Validate();

        AssertOutputDirectories(
            ["output/AUST-04/ERA5/evaluation/day", "output-CMIP6/AUST-04/ERA5/evaluation/day"],
            config.CreateDatasets());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CreateDatasets_UnspecifiedExperimentsSelectAllCompatibleExperiments(bool emptyFilter)
    {
        BarpaConfig config = CreateFilteredConfig();
        config.Experiments = emptyFilter ? [] : null;

        config.Validate();

        AssertOutputDirectories(
            [
                "output-CMIP6/AUS-15/ACCESS-CM2/historical/day",
                "output-CMIP6/AUS-15/ACCESS-CM2/ssp126/day",
                "output-CMIP6/AUS-15/ACCESS-CM2/ssp370/day",
                "output-CMIP6/AUS-15/ACCESS-CM2/ssp585/day"
            ], config.CreateDatasets());
    }

    [Fact]
    public void CreateDatasets_MultipleFiltersProduceAllSelectedCombinations()
    {
        BarpaConfig config = CreateValidConfig();
        config.Projects = ["output-CMIP6"];
        config.Domains = ["AUS-15", "AUST-15"];
        config.GCMs = ["ACCESS-CM2", "EC-Earth3"];
        config.Experiments = ["historical", "ssp585"];

        config.Validate();

        AssertOutputDirectories(
            [
                "output-CMIP6/AUS-15/ACCESS-CM2/historical/day",
                "output-CMIP6/AUS-15/ACCESS-CM2/ssp585/day",
                "output-CMIP6/AUS-15/EC-Earth3/historical/day",
                "output-CMIP6/AUS-15/EC-Earth3/ssp585/day",
                "output-CMIP6/AUST-15/ACCESS-CM2/historical/day",
                "output-CMIP6/AUST-15/ACCESS-CM2/ssp585/day",
                "output-CMIP6/AUST-15/EC-Earth3/historical/day",
                "output-CMIP6/AUST-15/EC-Earth3/ssp585/day"
            ], config.CreateDatasets());
    }

    [Fact]
    public void CreateDatasets_MixedSelectionsExcludeIncompatibleCombinations()
    {
        BarpaConfig config = CreateValidConfig();
        config.Projects = ["output", "output-CMIP6", "CORDEX-CMIP6"];
        config.Domains = ["AUST-04", "AUS-15", "AUS-20i"];
        config.GCMs = ["ERA5", "ACCESS-ESM1-5", "ACCESS-CM2"];
        config.Experiments = ["evaluation", "historical", "ssp585"];

        AssertOutputDirectories(
            [
                "output/AUST-04/ERA5/evaluation/day",
                "output-CMIP6/AUST-04/ERA5/evaluation/day",
                "output-CMIP6/AUST-04/ACCESS-ESM1-5/historical/day",
                "output-CMIP6/AUS-15/ERA5/evaluation/day",
                "output-CMIP6/AUS-15/ACCESS-ESM1-5/historical/day",
                "output-CMIP6/AUS-15/ACCESS-CM2/historical/day",
                "output-CMIP6/AUS-15/ACCESS-CM2/ssp585/day",
                "CORDEX-CMIP6/AUS-20i/ERA5/evaluation/day",
                "CORDEX-CMIP6/AUS-20i/ACCESS-ESM1-5/historical/day",
                "CORDEX-CMIP6/AUS-20i/ACCESS-CM2/historical/day",
                "CORDEX-CMIP6/AUS-20i/ACCESS-CM2/ssp585/day"
            ], config.CreateDatasets());
    }

    [Theory]
    [InlineData("output", "AUST-04", "ERA5", "evaluation", 1, "1hr")]
    [InlineData("output-CMIP6", "AUS-15", "ACCESS-CM2", "ssp585", 6, "6hr")]
    [InlineData("output-CMIP6", "AUST-04", "ACCESS-ESM1-5", "historical", 24, "day")]
    [InlineData("output-CMIP6", "AUST-15", "CESM2", "ssp126", 1, "1hr")]
    [InlineData("CORDEX-CMIP6", "AUS-20i", "EC-Earth3", "ssp585", 1, "1hr")]
    [InlineData("CORDEX-CMIP6", "AUS-20i", "ERA5", "evaluation", 24, "day")]
    public void ExplicitCompatibleSelection_ValidatesAndCreatesTheSelectedDataset(
        string project, string domain, string gcm, string experiment, int hours, string frequency)
    {
        BarpaConfig config = CreateFilteredConfig(hours);
        config.Projects = [project];
        config.Domains = [domain];
        config.GCMs = [gcm];
        config.Experiments = [experiment];

        config.Validate();

        AssertOutputDirectories([$"{project}/{domain}/{gcm}/{experiment}/{frequency}"], config.CreateDatasets());
    }

    [Theory]
    [InlineData("output-CMIP6", "AUS-15", "ACCESS-CM2", "historical", 3)]
    [InlineData("CORDEX-CMIP6", "AUS-20i", "ACCESS-CM2", "historical", 3)]
    [InlineData("CORDEX-CMIP6", "AUS-20i", "ACCESS-CM2", "historical", 6)]
    [InlineData("output", "AUS-15", "ERA5", "evaluation", 1)]
    [InlineData("output-CMIP6", "AUS-20i", "ERA5", "evaluation", 24)]
    [InlineData("CORDEX-CMIP6", "AUST-15", "ERA5", "evaluation", 24)]
    [InlineData("output", "AUST-04", "ACCESS-CM2", "historical", 1)]
    [InlineData("output-CMIP6", "AUST-04", "CESM2", "historical", 1)]
    [InlineData("output-CMIP6", "AUS-15", "ERA5", "historical", 1)]
    [InlineData("output-CMIP6", "AUS-15", "ACCESS-CM2", "evaluation", 1)]
    [InlineData("output-CMIP6", "AUS-15", "CESM2", "ssp585", 1)]
    [InlineData("output-CMIP6", "AUST-15", "CESM2", "ssp585", 1)]
    [InlineData("CORDEX-CMIP6", "AUS-20i", "ACCESS-ESM1-5", "ssp585", 24)]
    [InlineData("output-CMIP6", "AUST-04", "EC-Earth3", "ssp126", 1)]
    [InlineData("output-CMIP6", "AUST-04", "EC-Earth3", "ssp585", 1)]
    public void ExplicitIncompatibleSelection_IsRejectedAndProducesNoDatasets(
        string project, string domain, string gcm, string experiment, int hours)
    {
        BarpaConfig config = CreateFilteredConfig(hours);
        config.Projects = [project];
        config.Domains = [domain];
        config.GCMs = [gcm];
        config.Experiments = [experiment];

        Assert.ThrowsAny<ArgumentException>(config.Validate);
        Assert.Empty(config.CreateDatasets());
    }

    public static IEnumerable<object[]> InvalidFilters()
    {
        foreach (string dimension in new[] { "gcm", "domain", "project", "experiment" })
        {
            foreach (string identifier in new[] { "unknown", "" })
            {
                foreach (bool otherFiltersSpecified in new[] { false, true })
                    yield return [dimension, identifier, otherFiltersSpecified];
            }
        }
    }

    [Theory]
    [MemberData(nameof(InvalidFilters))]
    public void Validate_RejectsUnknownIdentifiersEvenWhenOtherFiltersAreUnspecified(
        string dimension, string identifier, bool otherFiltersSpecified)
    {
        BarpaConfig config = otherFiltersSpecified ? CreateFilteredConfig() : CreateValidConfig();
        SetFilter(config, dimension, [identifier]);

        Assert.ThrowsAny<ArgumentException>(config.Validate);
    }

    [Theory]
    [MemberData(nameof(InvalidFilters))]
    public void CreateDatasets_RejectsUnknownIdentifiers(
        string dimension, string identifier, bool otherFiltersSpecified)
    {
        BarpaConfig config = otherFiltersSpecified ? CreateFilteredConfig() : CreateValidConfig();
        SetFilter(config, dimension, [identifier]);

        Assert.ThrowsAny<ArgumentException>(() => config.CreateDatasets().ToList());
    }

    [Theory]
    [InlineData("gcm")]
    [InlineData("domain")]
    [InlineData("project")]
    [InlineData("experiment")]
    public void Validate_ChecksEveryExplicitIdentifier(string dimension)
    {
        BarpaConfig config = CreateFilteredConfig();
        IEnumerable<string> valid = dimension switch
        {
            "gcm" => config.GCMs!,
            "domain" => config.Domains!,
            "project" => config.Projects!,
            _ => config.Experiments!
        };
        SetFilter(config, dimension, [..valid, "unknown"]);

        Assert.ThrowsAny<ArgumentException>(config.Validate);
    }

    [Theory]
    [InlineData("gcm-experiment")]
    [InlineData("domain-frequency")]
    [InlineData("project-gcm")]
    public void Validate_RejectsPartialSelectionsWithNoCompatibleDatasets(string selection)
    {
        BarpaConfig config = CreateValidConfig(3);
        switch (selection)
        {
            case "gcm-experiment":
                config.GCMs = ["ERA5"];
                config.Experiments = ["historical"];
                break;
            case "domain-frequency":
                config.Domains = ["AUS-15"];
                break;
            case "project-gcm":
                config.Projects = ["output"];
                config.GCMs = ["ACCESS-CM2"];
                break;
        }

        Assert.Empty(config.CreateDatasets());
        Assert.ThrowsAny<ArgumentException>(config.Validate);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(8)]
    [InlineData(12)]
    [InlineData(48)]
    public void UnsupportedInputTimesteps_AreRejected(int hours)
    {
        BarpaConfig config = CreateValidConfig(hours);

        Assert.ThrowsAny<ArgumentException>(config.Validate);
        Assert.ThrowsAny<ArgumentException>(() => config.CreateDatasets().ToList());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(24)]
    public void Validate_TrunkUsesDailyInputAndOutput(int hours)
    {
        BarpaConfig config = CreateFilteredConfig(hours);
        config.Version = ModelVersion.Trunk;
        config.OutputTimeStepHours = hours;

        config.Validate();

        Assert.Equal(24, config.InputTimeStepHours);
        Assert.Equal(24, config.OutputTimeStepHours);
        AssertOutputDirectories(["output-CMIP6/AUS-15/ACCESS-CM2/historical/day"], config.CreateDatasets());
    }

    [Fact]
    public void Validate_RequiresAnExistingInputDirectory()
    {
        BarpaConfig config = CreateFilteredConfig();
        config.InputDirectory = Path.Combine(tempDirectory.AbsolutePath, "missing");

        Assert.ThrowsAny<ArgumentException>(config.Validate);
    }

    [Fact]
    public void Validate_RequiresPbsProjectEvenWhenArchiveProjectIsSelected()
    {
        BarpaConfig config = CreateFilteredConfig();
        config.Project = string.Empty;

        Assert.ThrowsAny<ArgumentException>(config.Validate);
    }

    [Fact]
    public void Validate_RejectsOutputTimestepFinerThanInput()
    {
        BarpaConfig config = CreateFilteredConfig(6);
        config.OutputTimeStepHours = 3;

        Assert.ThrowsAny<ArgumentException>(config.Validate);
    }

    [Fact]
    public void CreateDatasets_PassesInputDirectoryAndArchiveConfigurationToDataset()
    {
        BarpaConfig config = CreateFilteredConfig(6);
        config.InputDirectory = Path.Combine(tempDirectory.AbsolutePath, "custom-input");

        IClimateDataset dataset = Assert.Single(config.CreateDatasets());

        Assert.IsAssignableFrom<BarpaDataset>(dataset);
        Assert.Equal("AUS-15_ACCESS-CM2_historical_r4i1p1f1_BOM_BARPA-R_v1-r1_6hr", dataset.DatasetName);
        string expected = Path.Combine(config.InputDirectory,
            PortablePath("BARPA/output-CMIP6/DD/AUS-15/BOM/ACCESS-CM2/historical/r4i1p1f1/BARPA-R/v1-r1/6hr/tas/latest"));
        Assert.Equal(expected, dataset.GetInputFilesDirectory(ClimateVariable.Temperature));
    }

    [Theory]
    [InlineData("--project")]
    [InlineData("-p")]
    public void CommandLine_PbsProjectDoesNotSetArchiveProjectFilter(string pbsProjectOption)
    {
        BarpaConfig config = ParseArguments(
            "barpa", "--input-dir", tempDirectory.AbsolutePath,
            pbsProjectOption, PbsProject, "--input-timestep", "24", "--output-timestep", "24");

        Assert.Equal(PbsProject, config.Project);
        Assert.Empty(config.Projects ?? []);
        config.Validate();
        AssertOutputDirectories(ExpectedDefaultDirectories(24, "day"), config.CreateDatasets());
    }

    [Fact]
    public void CommandLine_BindsMultipleValuesForEachArchiveFilter()
    {
        BarpaConfig config = ParseArguments(
            "barpa", "--input-dir", tempDirectory.AbsolutePath, "--project", PbsProject,
            "--input-timestep", "24", "--output-timestep", "24",
            "--barpa-project", "output-CMIP6", "--domain", "AUS-15", "AUST-15",
            "--gcm", "ACCESS-CM2", "EC-Earth3", "--experiment", "historical", "ssp585");

        Assert.Equal(PbsProject, config.Project);
        Assert.Equal(new[] { "output-CMIP6" }, config.Projects);
        Assert.Equal(new[] { "AUS-15", "AUST-15" }, config.Domains);
        Assert.Equal(new[] { "ACCESS-CM2", "EC-Earth3" }, config.GCMs);
        Assert.Equal(new[] { "historical", "ssp585" }, config.Experiments);
        config.Validate();
        Assert.Equal(8, config.CreateDatasets().Count());
    }

    [Fact]
    public void CommandLine_BindsMultipleArchiveProjectsSeparatelyFromPbsProject()
    {
        BarpaConfig config = ParseArguments(
            "barpa", "--input-dir", tempDirectory.AbsolutePath, "--project", PbsProject,
            "--input-timestep", "1", "--output-timestep", "24",
            "--barpa-project", "output", "output-CMIP6", "--domain", "AUST-04",
            "--gcm", "ERA5", "--experiment", "evaluation");

        Assert.Equal(PbsProject, config.Project);
        config.Validate();
        AssertOutputDirectories(
            ["output/AUST-04/ERA5/evaluation/1hr", "output-CMIP6/AUST-04/ERA5/evaluation/1hr"],
            config.CreateDatasets());
    }

    private static BarpaConfig ParseArguments(params string[] arguments)
    {
        using Parser parser = new(settings => settings.HelpWriter = null);
        ParserResult<object> result = parser.ParseArguments(arguments, typeof(BarpaConfig));
        Parsed<object> parsed = Assert.IsType<Parsed<object>>(result);
        return Assert.IsType<BarpaConfig>(parsed.Value);
    }

    private BarpaConfig CreateValidConfig(int hours = 24) => new()
    {
        InputDirectory = tempDirectory.AbsolutePath,
        Project = PbsProject,
        Version = ModelVersion.Dave,
        InputTimeStepHours = hours,
        OutputTimeStepHours = 24
    };

    private BarpaConfig CreateFilteredConfig(int hours = 24)
    {
        BarpaConfig config = CreateValidConfig(hours);
        config.Projects = ["output-CMIP6"];
        config.Domains = ["AUS-15"];
        config.GCMs = ["ACCESS-CM2"];
        config.Experiments = ["historical"];
        return config;
    }

    private static void SetFilter(BarpaConfig config, string dimension, IEnumerable<string> values)
    {
        switch (dimension)
        {
            case "gcm": config.GCMs = values; break;
            case "domain": config.Domains = values; break;
            case "project": config.Projects = values; break;
            case "experiment": config.Experiments = values; break;
            default: throw new ArgumentException(nameof(dimension));
        }
    }

    private static void AssertOutputDirectories(IEnumerable<string> expected, IEnumerable<IClimateDataset> datasets)
    {
        Assert.Equal(expected.Select(PortablePath).OrderBy(p => p, StringComparer.Ordinal),
            datasets.Select(d => d.GetOutputDirectory()).OrderBy(p => p, StringComparer.Ordinal));
    }

    private static IEnumerable<string> ExpectedDefaultDirectories(int hours, string frequency)
    {
        yield return $"output/AUST-04/ERA5/evaluation/{frequency}";

        List<(string project, string domain)> regionalArchives = [];
        if (hours != 3)
        {
            regionalArchives.Add(("output-CMIP6", "AUS-15"));
            regionalArchives.Add(("output-CMIP6", "AUST-15"));
            yield return $"output-CMIP6/AUST-04/ERA5/evaluation/{frequency}";
            foreach (string gcm in new[] { "ACCESS-ESM1-5", "EC-Earth3" })
            {
                foreach (string experiment in new[] { "historical", "ssp370" })
                    yield return $"output-CMIP6/AUST-04/{gcm}/{experiment}/{frequency}";
            }
        }

        if (hours is 1 or 24)
            regionalArchives.Add(("CORDEX-CMIP6", "AUS-20i"));

        foreach ((string project, string domain) in regionalArchives)
        {
            foreach ((string gcm, string[] experiments) in regionalExperiments)
            {
                foreach (string experiment in experiments)
                    yield return $"{project}/{domain}/{gcm}/{experiment}/{frequency}";
            }
        }
    }

    private static string PortablePath(string path) => Path.Combine(path.Split('/'));
}

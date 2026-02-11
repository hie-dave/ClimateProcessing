using ClimateProcessing.Configuration;
using ClimateProcessing.Models;
using ClimateProcessing.Models.Cordex;
using Xunit;

namespace ClimateProcessing.Tests.Models;

public class CordexConfigTests
{
    private const string testInputDirectory = "/test/input/dir";
    private const string testProject = "test_project";

    [Fact]
    public void Constructor_InitializesProperties_WithDefaultValues()
    {
        CordexConfig config = new CordexConfig
        {
            InputDirectory = testInputDirectory,
            Project = testProject
        };

        Assert.Equal(testInputDirectory, config.InputDirectory);
        Assert.Equal(testProject, config.Project);
        Assert.Null(config.Activities);
        Assert.Null(config.Experiments);
        Assert.Null(config.GCMs);
        Assert.Null(config.Institutions);
        Assert.Null(config.Sources);
    }

    [Fact]
    public void GetVersions_WithNullVersions_ReturnsAllVersions()
    {
        CordexConfig config = new CordexConfig
        {
            InputDirectory = testInputDirectory,
            Project = testProject
        };

        IEnumerable<CordexVersion> versions = config.GetVersions();
        Assert.Equal(Enum.GetValues<CordexVersion>(), versions);
    }

    [Fact]
    public void GetVersions_WithEmptyVersions_ReturnsAllVersions()
    {
        CordexConfig config = new CordexConfig
        {
            InputDirectory = testInputDirectory,
            Project = testProject,
            Versions = new List<string>()
        };

        IEnumerable<CordexVersion> versions = config.GetVersions();
        Assert.Equal(Enum.GetValues<CordexVersion>(), versions);
    }

    [Fact]
    public void Validate_SucceedsForValidConfig()
    {
        using TempDirectory tempDirectory = TempDirectory.Create();
        CordexConfig config = CreateValidConfig(tempDirectory);
        config.Validate();
    }

    [Fact]
    public void Validate_ThrowsForNonDailyTimestep()
    {
        using TempDirectory tempDirectory = TempDirectory.Create();
        CordexConfig config = CreateValidConfig(tempDirectory);
        config.Version = ModelVersion.Dave;
        config.InputTimeStepHours = 1; // Invalid
        config.OutputTimeStepHours = 3;

        ArgumentException exception = Assert.Throws<ArgumentException>(config.Validate);
        Assert.Contains("timestep", exception.Message);
        Assert.Contains("CORDEX", exception.Message);
    }

    [Theory]
    [InlineData(CordexActivity.DD, CordexVersion.V1R1, CordexVersion.MrnbcAgcd)]
    [InlineData(CordexActivity.DD, CordexVersion.V1R1, CordexVersion.MrnbcBarra)]
    [InlineData(CordexActivity.DD, CordexVersion.V1R1, CordexVersion.QmeAgcd)]
    [InlineData(CordexActivity.DD, CordexVersion.V1R1, CordexVersion.QmeBarra)]
    [InlineData(CordexActivity.BiasCorrected, CordexVersion.V1R1, CordexVersion.MrnbcAgcd)]
    [InlineData(CordexActivity.BiasCorrected, CordexVersion.V1R1, CordexVersion.MrnbcBarra)]
    [InlineData(CordexActivity.BiasCorrected, CordexVersion.V1R1, CordexVersion.QmeAgcd)]
    [InlineData(CordexActivity.BiasCorrected, CordexVersion.V1R1, CordexVersion.QmeBarra)]
    [InlineData(CordexActivity.BiasCorrected, CordexVersion.V1R1, CordexVersion.QmeBarra, CordexVersion.MrnbcAgcd)]
    [InlineData(CordexActivity.BiasCorrected, CordexVersion.V1R1, CordexVersion.QmeAgcd, CordexVersion.QmeBarra)]
    public void Validate_ThrowsForUnusedVersion(CordexActivity activity, params CordexVersion[] versions)
    {
        // Generate a config with a version which is not supported by any
        using TempDirectory tempDirectory = TempDirectory.Create();
        CordexConfig config = CreateValidConfig(tempDirectory);
        config.Activities = [activity.ToActivityId()];
        config.Versions = versions.Select(v => v.ToVersionId()).ToArray();

        ArgumentException exception = Assert.Throws<ArgumentException>(config.Validate);
        Assert.Contains("version", exception.Message);
        Assert.Contains("not supported", exception.Message);
    }

    [Fact]
    public void Validate_ThrowsForUnusedActivity()
    {
        using TempDirectory tempDirectory = TempDirectory.Create();
        CordexConfig config = CreateValidConfig(tempDirectory);

        // Use all activities
        config.Activities = [];

        // Use a single version - not all activities support this version.
        config.Versions = [CordexVersion.V1R1.ToVersionId()];

        ArgumentException exception = Assert.Throws<ArgumentException>(config.Validate);
        Assert.Contains("activity", exception.Message);
        Assert.Contains("not supported", exception.Message);
    }

    [Theory]
    [InlineData(CordexVersion.V1R1)]
    [InlineData(CordexVersion.MrnbcAgcd)]
    [InlineData(CordexVersion.MrnbcBarra)]
    [InlineData(CordexVersion.QmeAgcd)]
    [InlineData(CordexVersion.QmeBarra)]
    [InlineData(CordexVersion.V1R1, CordexVersion.MrnbcAgcd)]
    [InlineData(CordexVersion.V1R1, CordexVersion.MrnbcAgcd, CordexVersion.MrnbcBarra)]
    public void GetVersions_WithSpecificVersions_ReturnsSpecifiedVersions(params CordexVersion[] versions)
    {
        CordexConfig config = new CordexConfig
        {
            InputDirectory = testInputDirectory,
            Project = testProject,
            Versions = versions.Select(v => v.ToVersionId()).ToArray()
        };

        List<CordexVersion> actual = config.GetVersions().ToList();

        Assert.Equal(versions.Length, actual.Count);
        foreach (CordexVersion version in versions)
            Assert.Contains(version, actual);
    }

    [Fact]
    public void GetActivities_WithNullActivities_ReturnsAllActivities()
    {
        CordexConfig config = new CordexConfig
        {
            InputDirectory = testInputDirectory,
            Project = testProject
        };

        IEnumerable<CordexActivity> activities = config.GetActivities();
        Assert.Equal(Enum.GetValues<CordexActivity>(), activities);
    }

    [Fact]
    public void GetActivities_WithEmptyActivities_ReturnsAllActivities()
    {
        CordexConfig config = new CordexConfig
        {
            InputDirectory = testInputDirectory,
            Project = testProject,
            Activities = new List<string>()
        };

        IEnumerable<CordexActivity> activities = config.GetActivities();
        Assert.Equal(Enum.GetValues<CordexActivity>(), activities);
    }

    [Fact]
    public void GetActivities_WithSpecificActivities_ReturnsSpecifiedActivities()
    {
        CordexConfig config = new CordexConfig
        {
            InputDirectory = testInputDirectory,
            Project = testProject,
            Activities = new[] { "DD" }
        };

        IEnumerable<CordexActivity> activities = config.GetActivities();

        Assert.Single(activities);
        Assert.Equal(CordexActivity.DD, activities.First());
    }

    [Fact]
    public void GetExperiments_WithNullExperiments_ReturnsAllExperiments()
    {
        CordexConfig config = new CordexConfig
        {
            InputDirectory = testInputDirectory,
            Project = testProject
        };

        IEnumerable<CordexExperiment> experiments = config.GetExperiments();
        Assert.Equal(Enum.GetValues<CordexExperiment>(), experiments);
    }

    [Fact]
    public void GetExperiments_WithEmptyExperiments_ReturnsAllExperiments()
    {
        CordexConfig config = new CordexConfig
        {
            InputDirectory = testInputDirectory,
            Project = testProject,
            Experiments = new List<string>()
        };

        IEnumerable<CordexExperiment> experiments = config.GetExperiments();
        Assert.Equal(Enum.GetValues<CordexExperiment>(), experiments);
    }

    [Fact]
    public void GetExperiments_WithSpecificExperiments_ReturnsSpecifiedExperiments()
    {
        CordexConfig config = new CordexConfig
        {
            InputDirectory = testInputDirectory,
            Project = testProject,
            Experiments = new[] { "historical", "ssp126" }
        };

        IEnumerable<CordexExperiment> experiments = config.GetExperiments();

        Assert.Equal(2, experiments.Count());
        Assert.Contains(CordexExperiment.Historical, experiments);
        Assert.Contains(CordexExperiment.Ssp126, experiments);
    }

    [Fact]
    public void GetGCMs_WithNullGCMs_ReturnsAllGCMs()
    {
        CordexConfig config = new CordexConfig
        {
            InputDirectory = testInputDirectory,
            Project = testProject
        };

        IEnumerable<CordexGcm> gcms = config.GetGCMs();
        Assert.Equal(Enum.GetValues<CordexGcm>(), gcms);
    }

    [Fact]
    public void GetGCMs_WithEmptyGCMs_ReturnsAllGCMs()
    {
        CordexConfig config = new CordexConfig
        {
            InputDirectory = testInputDirectory,
            Project = testProject,
            GCMs = new List<string>()
        };

        IEnumerable<CordexGcm> gcms = config.GetGCMs();
        Assert.Equal(Enum.GetValues<CordexGcm>(), gcms);
    }

    [Fact]
    public void GetGCMs_WithSpecificGCMs_ReturnsSpecifiedGCMs()
    {
        CordexConfig config = new CordexConfig
        {
            InputDirectory = testInputDirectory,
            Project = testProject,
            GCMs = new[] { "ACCESS-ESM2", "CESM2" }
        };

        IEnumerable<CordexGcm> gcms = config.GetGCMs();

        Assert.Equal(2, gcms.Count());
        Assert.Contains(CordexGcm.AccessCM2, gcms);
        Assert.Contains(CordexGcm.Cesm2, gcms);
    }

    [Fact]
    public void GetInstitutions_WithNullInstitutions_ReturnsAllInstitutions()
    {
        CordexConfig config = new CordexConfig
        {
            InputDirectory = testInputDirectory,
            Project = testProject
        };

        IEnumerable<CordexInstitution> institutions = config.GetInstitutions();

        Assert.Equal(Enum.GetValues<CordexInstitution>(), institutions);
    }

    [Fact]
    public void GetInstitutions_WithEmptyInstitutions_ReturnsAllInstitutions()
    {
        CordexConfig config = new CordexConfig
        {
            InputDirectory = testInputDirectory,
            Project = testProject,
            Institutions = new List<string>()
        };

        IEnumerable<CordexInstitution> institutions = config.GetInstitutions();

        Assert.Equal(Enum.GetValues<CordexInstitution>(), institutions);
    }

    [Fact]
    public void GetInstitutions_WithSpecificInstitutions_ReturnsSpecifiedInstitutions()
    {
        CordexConfig config = new CordexConfig
        {
            InputDirectory = testInputDirectory,
            Project = testProject,
            Institutions = new[] { "BOM" }
        };

        IEnumerable<CordexInstitution> institutions = config.GetInstitutions();

        Assert.Single(institutions);
        Assert.Equal(CordexInstitution.BOM, institutions.First());
    }

    [Fact]
    public void GetSources_WithNullSources_ReturnsAllSources()
    {
        CordexConfig config = new CordexConfig
        {
            InputDirectory = testInputDirectory,
            Project = testProject
        };

        IEnumerable<CordexSource> sources = config.GetSources();

        Assert.Equal(Enum.GetValues<CordexSource>(), sources);
    }

    [Fact]
    public void GetSources_WithEmptySources_ReturnsAllSources()
    {
        CordexConfig config = new CordexConfig
        {
            InputDirectory = testInputDirectory,
            Project = testProject,
            Sources = new List<string>()
        };

        IEnumerable<CordexSource> sources = config.GetSources();

        Assert.Equal(Enum.GetValues<CordexSource>(), sources);
    }

    [Fact]
    public void GetSources_WithSpecificSources_ReturnsSpecifiedSources()
    {
        CordexConfig config = new CordexConfig
        {
            InputDirectory = testInputDirectory,
            Project = testProject,
            Sources = new[] { "BARPA-R" }
        };

        IEnumerable<CordexSource> sources = config.GetSources();

        Assert.Single(sources);
        Assert.Equal(CordexSource.BarpaR, sources.First());
    }

    [Fact]
    public void CreateDatasets_WithDefaultSettings_ReturnsAllCombinations()
    {
        CordexConfig config = new CordexConfig
        {
            InputDirectory = testInputDirectory,
            Project = testProject
        };

        IEnumerable<IClimateDataset> datasets = config.CreateDatasets();

        int ncombinations = Enum.GetValues<CordexExperiment>().Length *
                            Enum.GetValues<CordexGcm>().Length *
                            Enum.GetValues<CordexInstitution>().Length *
                            Enum.GetValues<CordexSource>().Length;

        // Each combination is combined with all valid activity + version.
        // 2 activities: DD, bias-corrected
        // DD: 1 valid version (v1-r1)
        // BC: 4 valid versions
        // Total combinations = ncombinations * 1 + ncombinations * 4
        int expectedCount = ncombinations * 5;

        Assert.Equal(expectedCount, datasets.Count());
        Assert.All(datasets, d => Assert.IsType<CordexDataset>(d));
    }

    [Fact]
    public void CreateDatasets_WithFilteredSettings_ReturnsFilteredCombinations()
    {
        CordexConfig config = new CordexConfig
        {
            InputDirectory = testInputDirectory,
            Project = testProject,
            Activities = ["DD"],
            Experiments = ["historical"],
            GCMs = ["ACCESS-ESM2"],
            Institutions = ["BOM"],
            Sources = ["BARPA-R"]
        };

        IEnumerable<IClimateDataset> datasets = config.CreateDatasets();

        Assert.Single(datasets);
        CordexDataset dataset = Assert.IsType<CordexDataset>(datasets.First());

        // This is not ideal, but it may be better than nothing.
        Assert.Contains(CordexDomain.Aust05i.ToDomainId(), dataset.DatasetName);
        Assert.Contains(CordexInstitution.BOM.ToInstitutionId(), dataset.DatasetName);
        Assert.Contains(CordexGcm.AccessCM2.ToGcmId(), dataset.DatasetName);
        Assert.Contains(CordexExperiment.Historical.ToExperimentId(), dataset.DatasetName);
        Assert.Contains(CordexSource.BarpaR.ToSourceId(), dataset.DatasetName);
    }

    [Fact]
    public void CreateDatasets_WithInvalidActivity_ThrowsArgumentOutOfRangeException()
    {
        CordexConfig config = new CordexConfig
        {
            InputDirectory = testInputDirectory,
            Project = testProject,
            Activities = ["InvalidActivity"]
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => config.CreateDatasets().ToList());
    }

    [Fact]
    public void CreateDatasets_WithInvalidExperiment_ThrowsArgumentOutOfRangeException()
    {
        CordexConfig config = new CordexConfig
        {
            InputDirectory = testInputDirectory,
            Project = testProject,
            Experiments = ["InvalidExperiment"]
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => config.CreateDatasets().ToList());
    }

    [Fact]
    public void CreateDatasets_WithInvalidGCM_ThrowsArgumentOutOfRangeException()
    {
        CordexConfig config = new CordexConfig
        {
            InputDirectory = testInputDirectory,
            Project = testProject,
            GCMs = ["InvalidGCM"]
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => config.CreateDatasets().ToList());
    }

    [Fact]
    public void CreateDatasets_WithInvalidInstitution_ThrowsArgumentOutOfRangeException()
    {
        CordexConfig config = new CordexConfig
        {
            InputDirectory = testInputDirectory,
            Project = testProject,
            Institutions = ["InvalidInstitution"]
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => config.CreateDatasets().ToList());
    }

    [Fact]
    public void CreateDatasets_WithInvalidSource_ThrowsArgumentOutOfRangeException()
    {
        CordexConfig config = new CordexConfig
        {
            InputDirectory = testInputDirectory,
            Project = testProject,
            Sources = ["InvalidSource"]
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => config.CreateDatasets().ToList());
    }

    [Theory]
    [InlineData(CordexActivity.DD, CordexActivity.DD)]
    [InlineData(CordexActivity.BiasCorrected, CordexActivity.BiasCorrected)]
    [InlineData(CordexActivity.DD, CordexActivity.BiasCorrected, CordexActivity.DD)]
    public void CreateDatasets_WithDuplicatedActivity_DoesNotProduceDuplicates(params CordexActivity[] activities)
    {
        using TempDirectory tempDirectory = TempDirectory.Create();
        CordexConfig config = CreateValidConfig(tempDirectory);

        config.Activities = activities.Select(a => a.ToActivityId()).ToList();
        config.Versions = activities.Distinct().Select(a => Enum.GetValues<CordexVersion>().First(v => v.IsSupportedFor(a)).ToVersionId()).ToList();

        List<CordexDataset> datasets = config.CreateDatasets().Cast<CordexDataset>().ToList();
        Assert.Equal(activities.Distinct().Count(), datasets.Count);
    }

    [Theory]
    [InlineData(CordexVersion.V1R1, CordexVersion.V1R1)]
    [InlineData(CordexVersion.MrnbcAgcd, CordexVersion.MrnbcAgcd)]
    [InlineData(CordexVersion.MrnbcBarra, CordexVersion.MrnbcBarra)]
    [InlineData(CordexVersion.QmeAgcd, CordexVersion.QmeAgcd)]
    [InlineData(CordexVersion.QmeBarra, CordexVersion.QmeBarra)]
    [InlineData(CordexVersion.V1R1, CordexVersion.MrnbcAgcd, CordexVersion.V1R1)]
    [InlineData(CordexVersion.V1R1, CordexVersion.MrnbcAgcd, CordexVersion.MrnbcBarra, CordexVersion.V1R1)]
    public void CreateDatasets_WithDuplicatedVersion_DoesNotProduceDuplicates(params CordexVersion[] versions)
    {
        using TempDirectory tempDirectory = TempDirectory.Create();
        CordexConfig config = CreateValidConfig(tempDirectory);

        config.Versions = versions.Select(v => v.ToVersionId()).ToList();
        config.Activities = [];

        List<CordexDataset> datasets = config.CreateDatasets().Cast<CordexDataset>().ToList();
        Assert.Equal(versions.Distinct().Count(), datasets.Count);
    }

    [Theory]
    [InlineData(CordexExperiment.Historical, CordexExperiment.Historical)]
    [InlineData(CordexExperiment.Ssp126, CordexExperiment.Ssp126)]
    [InlineData(CordexExperiment.Ssp370, CordexExperiment.Ssp370)]
    [InlineData(CordexExperiment.Historical, CordexExperiment.Ssp126, CordexExperiment.Historical)]
    [InlineData(CordexExperiment.Historical, CordexExperiment.Ssp126, CordexExperiment.Ssp370, CordexExperiment.Ssp126)]
    public void CreateDatasets_WithDuplicatedExperiment_DoesNotProduceDuplicates(params CordexExperiment[] experiments)
    {
        using TempDirectory tempDirectory = TempDirectory.Create();
        CordexConfig config = CreateValidConfig(tempDirectory);

        config.Experiments = experiments.Select(e => e.ToExperimentId()).ToList();

        List<CordexDataset> datasets = config.CreateDatasets().Cast<CordexDataset>().ToList();
        Assert.Equal(experiments.Distinct().Count(), datasets.Count);
    }

    [Fact]
    public void CreateDatasets_WithDuplicatedGCM_DoesNotProduceDuplicates()
    {
        using TempDirectory tempDirectory = TempDirectory.Create();
        CordexConfig config = CreateValidConfig(tempDirectory);

        string gcm = CordexGcm.AccessCM2.ToGcmId();
        config.GCMs = [gcm, gcm];

        List<CordexDataset> datasets = config.CreateDatasets().Cast<CordexDataset>().ToList();
        Assert.Single(datasets);
    }

    [Theory]
    [InlineData(CordexInstitution.BOM, CordexInstitution.BOM)]
    [InlineData(CordexInstitution.CSIRO, CordexInstitution.CSIRO)]
    [InlineData(CordexInstitution.BOM, CordexInstitution.CSIRO, CordexInstitution.BOM)]
    public void CreateDatasets_WithDuplicatedInstitution_DoesNotProduceDuplicates(params CordexInstitution[] institutions)
    {
        using TempDirectory tempDirectory = TempDirectory.Create();
        CordexConfig config = CreateValidConfig(tempDirectory);

        config.Institutions = institutions.Select(i => i.ToInstitutionId()).ToList();

        List<CordexDataset> datasets = config.CreateDatasets().Cast<CordexDataset>().ToList();
        Assert.Equal(institutions.Distinct().Count(), datasets.Count);
    }

    [Theory]
    [InlineData(CordexSource.BarpaR, CordexSource.BarpaR)]
    [InlineData(CordexSource.Ccamv2203SN, CordexSource.Ccamv2203SN)]
    [InlineData(CordexSource.BarpaR, CordexSource.Ccamv2203SN, CordexSource.BarpaR)]
    public void CreateDatasets_WithDuplicatedSource_DoesNotProduceDuplicates(params CordexSource[] sources)
    {
        using TempDirectory tempDirectory = TempDirectory.Create();
        CordexConfig config = CreateValidConfig(tempDirectory);

        config.Sources = sources.Select(s => s.ToSourceId()).ToList();

        List<CordexDataset> datasets = config.CreateDatasets().Cast<CordexDataset>().ToList();
        Assert.Equal(sources.Distinct().Count(), datasets.Count);
    }

    [Theory]
    [InlineData(CordexInstitution.CSIRO, CordexSource.BarpaR)]
    public void Validate_ThrowsForUnsupportedCombination(CordexInstitution institution, CordexSource source)
    {
        using TempDirectory tempDirectory = TempDirectory.Create();
        CordexConfig config = CreateValidConfig(tempDirectory);

        config.Institutions = [institution.ToInstitutionId()];
        config.Sources = [source.ToSourceId()];

        ArgumentException exception = Assert.Throws<ArgumentException>(config.Validate);
        Assert.Contains("combination", exception.Message);
        Assert.Contains(institution.ToInstitutionId(), exception.Message);
        Assert.Contains(source.ToSourceId(), exception.Message);
    }

    private CordexConfig CreateValidConfig(TempDirectory workingDirectory)
    {
        return new CordexConfig
        {
            Version = ModelVersion.Trunk,
            InputDirectory = workingDirectory.AbsolutePath,
            InputTimeStepHours = 24,
            OutputTimeStepHours = 24,
            Project = testProject,
            Activities = [CordexActivity.DD.ToActivityId()],
            Versions = [CordexVersion.V1R1.ToVersionId()],
            Experiments = [CordexExperiment.Historical.ToExperimentId()],
            GCMs = [CordexGcm.AccessCM2.ToGcmId()],
            Institutions = [CordexInstitution.BOM.ToInstitutionId()],
            Sources = [CordexSource.BarpaR.ToSourceId()]
        };
    }
}

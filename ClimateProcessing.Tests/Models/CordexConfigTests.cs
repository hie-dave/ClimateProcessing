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

        int expectedCount = Enum.GetValues<CordexActivity>().Length *
                           Enum.GetValues<CordexExperiment>().Length *
                           Enum.GetValues<CordexGcm>().Length *
                           Enum.GetValues<CordexInstitution>().Length *
                           Enum.GetValues<CordexSource>().Length;

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
}

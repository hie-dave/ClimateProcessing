using ClimateProcessing.Models;
using ClimateProcessing.Models.Barra2;
using Xunit;

namespace ClimateProcessing.Tests.Models;

public sealed class Barra2ConfigTests : IDisposable
{
    private readonly TempDirectory tempDirectory;
    private readonly string validProject;

    public Barra2ConfigTests()
    {
        tempDirectory = TempDirectory.Create(GetType().Name);
        validProject = "test_project";
    }

    public void Dispose()
    {
        tempDirectory.Dispose();
    }

    private Barra2Config CreateValidConfig()
    {
        return new Barra2Config
        {
            InputDirectory = tempDirectory.AbsolutePath,
            Project = validProject,
            // Use Trunk to mirror NarClim2 tests and simplify timestep validation
            Version = ModelVersion.Trunk,
            InputTimeStepHours = 24,
            OutputTimeStepHours = 24
        };
    }

    [Fact]
    public void CreateDatasets_WithNoFilters_ReturnsAllCombinations()
    {
        var config = CreateValidConfig();

        var datasets = config.CreateDatasets().Cast<Barra2Dataset>().ToList();

        int expectedCount = Enum.GetValues<Barra2Domain>().Length
                            * 1 // default frequency is Hour1 only
                            * Enum.GetValues<Barra2Grid>().Length
                            * Enum.GetValues<Barra2Variant>().Length;
        Assert.Equal(expectedCount, datasets.Count);

        // Spot-check: every DatasetName should contain a valid token from each dimension
        string[] domainTokens = Enum.GetValues<Barra2Domain>().Select(Barra2DomainExtensions.ToString).ToArray();
        string[] gridTokens = Enum.GetValues<Barra2Grid>().Select(Barra2GridExtensions.ToString).ToArray();
        string[] variantTokens = Enum.GetValues<Barra2Variant>().Select(Barra2VariantExtensions.ToString).ToArray();
        string freqToken = Barra2FrequencyExtensions.ToString(Barra2Frequency.Hour1);

        Assert.All(datasets, ds => Assert.Contains(domainTokens, t => ds.DatasetName.Contains(t)));
        Assert.All(datasets, ds => Assert.Contains(gridTokens, t => ds.DatasetName.Contains(t)));
        Assert.All(datasets, ds => Assert.Contains(variantTokens, t => ds.DatasetName.Contains(t)));
        Assert.All(datasets, ds => Assert.Contains($"_{freqToken}", ds.DatasetName));
    }

    [Theory]
    [InlineData(Barra2Domain.Aus11)]
    [InlineData(Barra2Domain.Aus22)]
    [InlineData(Barra2Domain.Aust04)]
    [InlineData(Barra2Domain.Aust11)]
    public void CreateDatasets_WithSingleDomain_FiltersCorrectly(Barra2Domain domain)
    {
        var config = CreateValidConfig();
        config.Domains = [Barra2DomainExtensions.ToString(domain)];

        var datasets = config.CreateDatasets().Cast<Barra2Dataset>().ToList();

        Assert.NotEmpty(datasets);
        Assert.All(datasets, ds => Assert.StartsWith(Barra2DomainExtensions.ToString(domain), ds.DatasetName));
    }

    [Theory]
    [InlineData(Barra2Frequency.Hour1)]
    [InlineData(Barra2Frequency.Hour3)]
    [InlineData(Barra2Frequency.Hour6)]
    [InlineData(Barra2Frequency.Daily)]
    [InlineData(Barra2Frequency.Monthly)]
    [InlineData(Barra2Frequency.Constant)]
    public void CreateDatasets_WithSingleFrequency_FiltersCorrectly(Barra2Frequency frequency)
    {
        var config = CreateValidConfig();
        config.Frequencies = [Barra2FrequencyExtensions.ToString(frequency)];

        var datasets = config.CreateDatasets().Cast<Barra2Dataset>().ToList();

        Assert.NotEmpty(datasets);
        Assert.All(datasets, ds => Assert.EndsWith($"_{Barra2FrequencyExtensions.ToString(frequency)}", ds.DatasetName));
    }

    [Theory]
    [InlineData(Barra2Grid.R2)]
    [InlineData(Barra2Grid.RE2)]
    [InlineData(Barra2Grid.C2)]
    public void CreateDatasets_WithSingleGrid_FiltersCorrectly(Barra2Grid grid)
    {
        var config = CreateValidConfig();
        config.Grids = [Barra2GridExtensions.ToString(grid)];

        var datasets = config.CreateDatasets().Cast<Barra2Dataset>().ToList();

        Assert.NotEmpty(datasets);
        Assert.All(datasets, ds => Assert.Contains($"_{Barra2GridExtensions.ToString(grid)}_", ds.DatasetName));
    }

    [Theory]
    [InlineData(Barra2Variant.HRes)]
    [InlineData(Barra2Variant.Eda)]
    public void CreateDatasets_WithSingleVariant_FiltersCorrectly(Barra2Variant variant)
    {
        var config = CreateValidConfig();
        config.Variants = [Barra2VariantExtensions.ToString(variant)];

        var datasets = config.CreateDatasets().Cast<Barra2Dataset>().ToList();

        Assert.NotEmpty(datasets);
        Assert.All(datasets, ds => Assert.Contains($"_{Barra2VariantExtensions.ToString(variant)}_BOM_", ds.DatasetName));
    }

    [Theory]
    [InlineData(Barra2Domain.Aus11, Barra2Frequency.Hour1, Barra2Grid.R2, Barra2Variant.HRes)]
    [InlineData(Barra2Domain.Aust11, Barra2Frequency.Monthly, Barra2Grid.C2, Barra2Variant.Eda)]
    public void CreateDatasets_WithMultipleFilters_AppliesAllFilters(
        Barra2Domain domain,
        Barra2Frequency frequency,
        Barra2Grid grid,
        Barra2Variant variant)
    {
        var config = CreateValidConfig();
        config.Domains = [Barra2DomainExtensions.ToString(domain)];
        config.Frequencies = [Barra2FrequencyExtensions.ToString(frequency)];
        config.Grids = [Barra2GridExtensions.ToString(grid)];
        config.Variants = [Barra2VariantExtensions.ToString(variant)];

        var datasets = config.CreateDatasets().Cast<Barra2Dataset>().ToList();

        Assert.Single(datasets);
        string expected = $"{Barra2DomainExtensions.ToString(domain)}_ERA5_historical_{Barra2VariantExtensions.ToString(variant)}_BOM_{Barra2GridExtensions.ToString(grid)}_v1_{Barra2FrequencyExtensions.ToString(frequency)}";
        Assert.Equal(expected, datasets[0].DatasetName);
    }

    [Fact]
    public void CreateDatasets_DefaultFrequency_IsHour1()
    {
        var config = CreateValidConfig();
        // Do not set Frequencies
        var datasets = config.CreateDatasets().Cast<Barra2Dataset>().ToList();
        Assert.NotEmpty(datasets);
        Assert.All(datasets, ds => Assert.EndsWith($"_{Barra2FrequencyExtensions.ToString(Barra2Frequency.Hour1)}", ds.DatasetName));
    }

    [Theory]
    [InlineData("invalid-domain")]
    public void CreateDatasets_WithInvalidDomain_Throws(string invalid)
    {
        var config = CreateValidConfig();
        config.Domains = [invalid];
        Assert.Throws<ArgumentException>(() => config.CreateDatasets().ToList());
    }

    [Theory]
    [InlineData("invalid-frequency")]
    public void CreateDatasets_WithInvalidFrequency_Throws(string invalid)
    {
        var config = CreateValidConfig();
        config.Frequencies = [invalid];
        Assert.Throws<ArgumentException>(() => config.CreateDatasets().ToList());
    }

    [Theory]
    [InlineData("invalid-grid")]
    public void CreateDatasets_WithInvalidGrid_Throws(string invalid)
    {
        var config = CreateValidConfig();
        config.Grids = [invalid];
        Assert.Throws<ArgumentException>(() => config.CreateDatasets().ToList());
    }

    [Theory]
    [InlineData("invalid-variant")]
    public void CreateDatasets_WithInvalidVariant_Throws(string invalid)
    {
        var config = CreateValidConfig();
        config.Variants = [invalid];
        Assert.Throws<ArgumentException>(() => config.CreateDatasets().ToList());
    }

    [Fact]
    public void Validate_WithValidConfig_DoesNotThrow()
    {
        var config = CreateValidConfig();
        var ex = Record.Exception(() => config.Validate());
        Assert.Null(ex);
    }

    [Fact]
    public void Validate_WithInvalidInputDirectory_Throws()
    {
        var config = CreateValidConfig();
        config.InputDirectory = Path.Combine(tempDirectory.AbsolutePath, "does-not-exist");
        Assert.False(Directory.Exists(config.InputDirectory));
        Assert.Throws<ArgumentException>(() => config.Validate());
    }
}

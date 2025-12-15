using System;
using System.IO;
using System.Linq;
using ClimateProcessing.Models;
using ClimateProcessing.Models.Barra2;
using ClimateProcessing.Services.Processors;
using ClimateProcessing.Tests.Mocks;
using Xunit;

namespace ClimateProcessing.Tests.Models;

public class Barra2DatasetTests : IDisposable
{
    private readonly TempDirectory tempDirectory;
    private readonly string baseDir;

    private readonly Barra2Domain domain = Barra2Domain.Aus11;
    private readonly Barra2Frequency frequency = Barra2Frequency.Hour1;
    private readonly Barra2Grid grid = Barra2Grid.R2;
    private readonly Barra2Variant variant = Barra2Variant.HRes;

    public Barra2DatasetTests()
    {
        tempDirectory = TempDirectory.Create(GetType().Name);

        // Build base directory consistent with Barra2Dataset.GetInputFilesDirectory
        // <base>/BARRA2/output/reanalysis/<domain>/BOM/ERA5/historical/<variant>/<grid>/v1/<freq>/<variable>/latest
        baseDir = Path.Combine(
            tempDirectory.AbsolutePath,
            "BARRA2",
            "output",
            "reanalysis",
            Barra2DomainExtensions.ToString(domain),
            "BOM",
            "ERA5",
            "historical",
            Barra2VariantExtensions.ToString(variant),
            Barra2GridExtensions.ToString(grid),
            "v1",
            Barra2FrequencyExtensions.ToString(frequency));

        // Create directories and files for variables we will test
        foreach (string var in new[] { "tas", "pr" })
        {
            string varDir = Path.Combine(baseDir, var, "latest");
            Directory.CreateDirectory(varDir);

            // Create two monthly files 197901 and 197902
            CreateTestFile(Path.Combine(varDir, $"{var}_{Barra2DomainExtensions.ToString(domain)}_ERA5_historical_{Barra2VariantExtensions.ToString(variant)}_BOM_{Barra2GridExtensions.ToString(grid)}_v1_{Barra2FrequencyExtensions.ToString(frequency)}_197901-197901.nc"));
            CreateTestFile(Path.Combine(varDir, $"{var}_{Barra2DomainExtensions.ToString(domain)}_ERA5_historical_{Barra2VariantExtensions.ToString(variant)}_BOM_{Barra2GridExtensions.ToString(grid)}_v1_{Barra2FrequencyExtensions.ToString(frequency)}_197902-197902.nc"));
        }
    }

    public void Dispose()
    {
        tempDirectory.Dispose();
    }

    private static void CreateTestFile(string filePath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        File.WriteAllText(filePath, string.Empty);
    }

    private Barra2Dataset CreateDataset() => new(
        tempDirectory.AbsolutePath,
        domain: domain,
        frequency: frequency,
        grid: grid,
        variant: variant);

    [Theory]
    [InlineData(ClimateVariable.Temperature, 2)]
    [InlineData(ClimateVariable.Precipitation, 2)]
    public void GetInputFiles_ReturnsCorrectFiles(ClimateVariable variable, int expectedCount)
    {
        var dataset = CreateDataset();
        var files = dataset.GetInputFiles(variable).ToList();

        Assert.Equal(expectedCount, files.Count);

        string name = dataset.GetVariableInfo(variable).Name;
        Assert.Contains(files, f => Path.GetFileName(f).StartsWith($"{name}_") && f.Contains("197901-197901"));
        Assert.Contains(files, f => Path.GetFileName(f).StartsWith($"{name}_") && f.Contains("197902-197902"));

        // Ensure sorted by start date ascending
        Assert.Collection(files,
            f => Assert.Contains("197901-197901", f),
            f => Assert.Contains("197902-197902", f));
    }

    [Fact]
    public void GetInputFiles_WithInvalidFilename_Throws()
    {
        var dataset = CreateDataset();

        string varDir = dataset.GetInputFilesDirectory(ClimateVariable.Temperature);
        // Drop an invalid file that matches *.nc but wrong date
        CreateTestFile(Path.Combine(varDir, "tas_invalid_foo.nc"));

        // Enumerating should throw due to date parsing inside OrderBy
        Assert.Throws<ArgumentException>(() => dataset.GetInputFiles(ClimateVariable.Temperature).ToList());
    }

    [Theory]
    [InlineData(ClimateVariable.Temperature, "tas", "K")]
    [InlineData(ClimateVariable.Precipitation, "pr", "kg m-2 s-1")]
    [InlineData(ClimateVariable.SpecificHumidity, "huss", "1")]
    [InlineData(ClimateVariable.SurfacePressure, "ps", "Pa")]
    [InlineData(ClimateVariable.ShortwaveRadiation, "rsds", "W m-2")]
    [InlineData(ClimateVariable.WindSpeed, "sfcWind", "m s-1")]
    [InlineData(ClimateVariable.MinTemperature, "tasmin", "K")]
    [InlineData(ClimateVariable.MaxTemperature, "tasmax", "K")]
    public void GetVariableInfo_ReturnsCorrectInfo(ClimateVariable variable, string expectedName, string expectedUnits)
    {
        var dataset = CreateDataset();
        var info = dataset.GetVariableInfo(variable);
        Assert.Equal(expectedName, info.Name);
        Assert.Equal(expectedUnits, info.Units);
    }

    [Fact]
    public void GetVariableInfo_TemperatureWithNonHourlyFrequency_Throws()
    {
        var dataset = new Barra2Dataset(
            tempDirectory.AbsolutePath,
            domain: domain,
            frequency: Barra2Frequency.Daily, // not Hour1
            grid: grid,
            variant: variant);

        var ex = Assert.Throws<ArgumentException>(() => dataset.GetVariableInfo(ClimateVariable.Temperature));
        Assert.Contains("only 1hr is supported", ex.Message);
    }

    [Fact]
    public void GetVariableInfo_ForInvalidVariable_Throws()
    {
        var dataset = CreateDataset();
        var invalid = (ClimateVariable)9999;
        var ex = Assert.Throws<ArgumentException>(() => dataset.GetVariableInfo(invalid));
        Assert.Contains("not supported in BARRA2 dataset", ex.Message);
    }

    [Theory]
    [InlineData(ClimateVariable.Temperature, "tas")]
    [InlineData(ClimateVariable.Precipitation, "pr")]
    public void GenerateOutputFileName_ReturnsCorrectPattern(ClimateVariable variable, string varName)
    {
        var dataset = CreateDataset();
        var filename = dataset.GenerateOutputFileName(variable, dataset.GetVariableInfo(variable));

        string expected = $"{varName}_{Barra2DomainExtensions.ToString(domain)}_ERA5_historical_{Barra2VariantExtensions.ToString(variant)}_BOM_{Barra2GridExtensions.ToString(grid)}_v1_{Barra2FrequencyExtensions.ToString(frequency)}_197901-197902.nc";
        Assert.Equal(expected, filename);
    }

    [Theory]
    [InlineData(ClimateVariable.Temperature, "tav")]
    [InlineData(ClimateVariable.Precipitation, "precip")]
    public void GenerateOutputFileName_UsesRenamedVariable(ClimateVariable variable, string newName)
    {
        var dataset = CreateDataset();
        var old = dataset.GetVariableInfo(variable);
        var renamed = new VariableInfo(newName, old.Units);
        var filename = dataset.GenerateOutputFileName(variable, renamed);

        string expected = $"{newName}_{Barra2DomainExtensions.ToString(domain)}_ERA5_historical_{Barra2VariantExtensions.ToString(variant)}_BOM_{Barra2GridExtensions.ToString(grid)}_v1_{Barra2FrequencyExtensions.ToString(frequency)}_197901-197902.nc";
        Assert.Equal(expected, filename);
    }

    [Fact]
    public void GenerateOutputFileName_ForMissingVariable_Throws()
    {
        // Dataset pointing to empty directory
        var emptyDir = Path.Combine(tempDirectory.AbsolutePath, "empty");
        Directory.CreateDirectory(emptyDir);
        var empty = new Barra2Dataset(emptyDir, domain, frequency, grid, variant);

        var ex = Assert.Throws<ArgumentException>(() =>
            empty.GenerateOutputFileName(ClimateVariable.Temperature, new VariableInfo("tas", "K")));
        Assert.Contains("No input files found for variable", ex.Message);
    }

    [Theory]
    [InlineData(Barra2Domain.Aus11, Barra2Grid.R2, Barra2Variant.HRes, Barra2Frequency.Hour1)]
    [InlineData(Barra2Domain.Aus22, Barra2Grid.RE2, Barra2Variant.Eda, Barra2Frequency.Daily)]
    public void DatasetName_ReturnsExpected(Barra2Domain d, Barra2Grid g, Barra2Variant v, Barra2Frequency f)
    {
        var dataset = new Barra2Dataset("/input", d, f, g, v);
        string expected = $"{Barra2DomainExtensions.ToString(d)}_ERA5_historical_{Barra2VariantExtensions.ToString(v)}_BOM_{Barra2GridExtensions.ToString(g)}_v1_{Barra2FrequencyExtensions.ToString(f)}";
        Assert.Equal(expected, dataset.DatasetName);
    }

    [Theory]
    [InlineData(Barra2Domain.Aus11, Barra2Grid.R2, Barra2Variant.HRes, Barra2Frequency.Hour1)]
    [InlineData(Barra2Domain.Aust11, Barra2Grid.C2, Barra2Variant.Eda, Barra2Frequency.Monthly)]
    public void GetOutputDirectory_ReturnsExpected(Barra2Domain d, Barra2Grid g, Barra2Variant v, Barra2Frequency f)
    {
        var dataset = new Barra2Dataset("/input", d, f, g, v);
        string expected = Path.Combine(
            Barra2DomainExtensions.ToString(d),
            Barra2GridExtensions.ToString(g),
            Barra2VariantExtensions.ToString(v),
            Barra2FrequencyExtensions.ToString(f));
        Assert.Equal(expected, dataset.GetOutputDirectory());
    }

    [Fact]
    public void GetProcessors_WhenOutputTimeStepIsDaily_IncludesMinAndMaxTemperature()
    {
        var context = new TestJobCreationContext(ModelVersion.Trunk);
        context.MutableConfig.OutputTimeStepHours = 24;

        var dataset = new Barra2Dataset("/input", domain, frequency, grid, variant);

        var processors = dataset
            .GetProcessors(context)
            .OfType<StandardVariableProcessor>()
            .ToList();
        var variables = processors.Select(p => p.TargetVariable).ToHashSet();

        Assert.Contains(ClimateVariable.MinTemperature, variables);
        Assert.Contains(ClimateVariable.MaxTemperature, variables);
    }

    [Fact]
    public void GetProcessors_WhenOutputTimeStepIsSubdaily_DoesNotIncludeMinOrMaxTemperature()
    {
        var context = new TestJobCreationContext(ModelVersion.Trunk);
        context.MutableConfig.OutputTimeStepHours = 3;

        var dataset = new Barra2Dataset("/input", domain, frequency, grid, variant);

        var processors = dataset
            .GetProcessors(context)
            .OfType<StandardVariableProcessor>()
            .ToList();
        var variables = processors.Select(p => p.TargetVariable).ToHashSet();

        Assert.DoesNotContain(ClimateVariable.MinTemperature, variables);
        Assert.DoesNotContain(ClimateVariable.MaxTemperature, variables);
    }
}

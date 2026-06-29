using ClimateProcessing.Models;
using ClimateProcessing.Models.Cmip6;
using ClimateProcessing.Services.Processors;
using ClimateProcessing.Tests.Mocks;
using Xunit;

namespace ClimateProcessing.Tests.Models;

public class Cmip6DatasetTests : IDisposable
{
    private readonly TempDirectory tempDirectory;

    public Cmip6DatasetTests()
    {
        tempDirectory = TempDirectory.Create(GetType().Name);
    }

    public void Dispose()
    {
        tempDirectory.Dispose();
    }

    [Theory]
    [InlineData(Cmip6Gcm.AccessEsm15, Cmip6Experiment.Ssp119, "ACCESS-ESM1-5_ssp119")]
    [InlineData(Cmip6Gcm.NorEsm2MM, Cmip6Experiment.Ssp245, "NorESM2-MM_ssp245")]
    [InlineData(Cmip6Gcm.ECEarth3, Cmip6Experiment.Ssp245, "EC-Earth3_ssp245")]
    public void DatasetName_ReturnsExpectedName(Cmip6Gcm gcm, Cmip6Experiment experiment, string expected)
    {
        Cmip6Dataset dataset = CreateDataset(gcm, experiment);

        Assert.Equal(expected, dataset.DatasetName);
    }

    [Fact]
    public void Constructor_SetsGcmAndExperiment()
    {
        Cmip6Dataset dataset = CreateDataset(Cmip6Gcm.ECEarth3, Cmip6Experiment.Ssp119);

        Assert.Equal(Cmip6Gcm.ECEarth3, dataset.Gcm);
        Assert.Equal(Cmip6Experiment.Ssp119, dataset.Experiment);
    }

    [Theory]
    [InlineData(ClimateVariable.RelativeHumidity, "hurs")]
    [InlineData(ClimateVariable.Precipitation, "pr")]
    [InlineData(ClimateVariable.ShortwaveRadiation, "rsds")]
    [InlineData(ClimateVariable.WindSpeed, "sfcWind")]
    [InlineData(ClimateVariable.SurfacePressure, "ps")]
    [InlineData(ClimateVariable.MaxTemperature, "tasmax")]
    [InlineData(ClimateVariable.MinTemperature, "tasmin")]
    public void GetInputFilesDirectory_ReturnsExpectedDirectory(ClimateVariable variable, string variableName)
    {
        Cmip6Dataset dataset = CreateDataset(Cmip6Gcm.AccessEsm15, Cmip6Experiment.Ssp245);

        string expected = Path.Combine(
            tempDirectory.AbsolutePath,
            "ACCESS-ESM1-5",
            "ssp245",
            variableName);
        Assert.Equal(expected, dataset.GetInputFilesDirectory(variable));
    }

    [Theory]
    [InlineData(ClimateVariable.SpecificHumidity)]
    [InlineData(ClimateVariable.Temperature)]
    [InlineData(ClimateVariable.Vpd)]
    [InlineData(ClimateVariable.MinRelativeHumidity)]
    [InlineData(ClimateVariable.MaxRelativeHumidity)]
    public void GetInputFilesDirectory_ThrowsForUnsupportedVariable(ClimateVariable variable)
    {
        Cmip6Dataset dataset = CreateDataset();

        Assert.Throws<ArgumentException>(() => dataset.GetInputFilesDirectory(variable));
    }

    [Theory]
    [InlineData(Cmip6Gcm.AccessEsm15, Cmip6Experiment.Ssp119, "ACCESS-ESM1-5/ssp119")]
    [InlineData(Cmip6Gcm.NorEsm2MM, Cmip6Experiment.Ssp245, "NorESM2-MM/ssp245")]
    public void GetOutputDirectory_ReturnsExpectedDirectory(Cmip6Gcm gcm, Cmip6Experiment experiment, string expected)
    {
        Cmip6Dataset dataset = CreateDataset(gcm, experiment);

        Assert.Equal(Path.Combine(expected.Split('/')), dataset.GetOutputDirectory());
    }

    [Theory]
    [InlineData(ClimateVariable.RelativeHumidity, "hurs", "%")]
    [InlineData(ClimateVariable.Precipitation, "pr", "kg m-2 s-1")]
    [InlineData(ClimateVariable.ShortwaveRadiation, "rsds", "W m-2")]
    [InlineData(ClimateVariable.WindSpeed, "sfcWind", "m s-1")]
    [InlineData(ClimateVariable.SurfacePressure, "ps", "Pa")]
    [InlineData(ClimateVariable.MaxTemperature, "tasmax", "K")]
    [InlineData(ClimateVariable.MinTemperature, "tasmin", "K")]
    public void GetVariableInfo_ReturnsExpectedMetadata(ClimateVariable variable, string expectedName, string expectedUnits)
    {
        Cmip6Dataset dataset = CreateDataset();

        VariableInfo info = dataset.GetVariableInfo(variable);

        Assert.Equal(expectedName, info.Name);
        Assert.Equal(expectedUnits, info.Units);
    }

    [Theory]
    [InlineData(ClimateVariable.SpecificHumidity)]
    [InlineData(ClimateVariable.Temperature)]
    [InlineData(ClimateVariable.Vpd)]
    [InlineData(ClimateVariable.MinRelativeHumidity)]
    [InlineData(ClimateVariable.MaxRelativeHumidity)]
    public void GetVariableInfo_ThrowsForUnsupportedVariable(ClimateVariable variable)
    {
        Cmip6Dataset dataset = CreateDataset();

        ArgumentException ex = Assert.Throws<ArgumentException>(() => dataset.GetVariableInfo(variable));
        Assert.Contains("not supported in CMIP6 dataset", ex.Message);
    }

    [Fact]
    public void GetInputFiles_ReturnsEmptyCollectionForMissingDirectory()
    {
        Cmip6Dataset dataset = CreateDataset();

        Assert.Empty(dataset.GetInputFiles(ClimateVariable.Precipitation));
    }

    [Fact]
    public void GetInputFiles_ReturnsOnlyNetcdfFilesSortedByStartDate()
    {
        Cmip6Dataset dataset = CreateDataset(Cmip6Gcm.NorEsm2MM, Cmip6Experiment.Ssp245);

        CreateInputFile(dataset, ClimateVariable.WindSpeed, "sfcWind_day_NorESM2-MM_ssp245_r1i1p1f1_gn_20810101-20901231.nc");
        CreateInputFile(dataset, ClimateVariable.WindSpeed, "sfcWind_day_NorESM2-MM_ssp245_r1i1p1f1_gn_20710101-20801231.nc");
        CreateInputFile(dataset, ClimateVariable.WindSpeed, "sfcWind_day_NorESM2-MM_ssp245_r1i1p1f1_gn_20910101-21001231.txt");

        List<string> files = dataset.GetInputFiles(ClimateVariable.WindSpeed).ToList();

        Assert.Collection(files,
            file => Assert.Contains("20710101-20801231", file),
            file => Assert.Contains("20810101-20901231", file));
    }

    [Fact]
    public void GetInputFiles_ThrowsWhenNetcdfFileHasInvalidDateRange()
    {
        Cmip6Dataset dataset = CreateDataset();
        CreateInputFile(dataset, ClimateVariable.Precipitation, "pr_day_ACCESS-ESM1-5_ssp245_r1i1p1f1_gn_bad.nc");

        Assert.Throws<ArgumentException>(() => dataset.GetInputFiles(ClimateVariable.Precipitation).ToList());
    }

    [Fact]
    public void GenerateOutputFileName_ReturnsExpectedFileName()
    {
        Cmip6Dataset dataset = CreateDataset(Cmip6Gcm.NorEsm2MM, Cmip6Experiment.Ssp245);
        CreateInputFile(dataset, ClimateVariable.WindSpeed, "sfcWind_day_NorESM2-MM_ssp245_r1i1p1f1_gn_20710101-20801231.nc");
        CreateInputFile(dataset, ClimateVariable.WindSpeed, "sfcWind_day_NorESM2-MM_ssp245_r1i1p1f1_gn_20810101-20901231.nc");

        string fileName = dataset.GenerateOutputFileName(ClimateVariable.WindSpeed, dataset.GetVariableInfo(ClimateVariable.WindSpeed));

        Assert.Equal("sfcWind_day_NorESM2-MM_ssp245_r1i1p1f1_gn_207101-209012.nc", fileName);
    }

    [Fact]
    public void GenerateOutputFileName_UsesRenamedVariable()
    {
        Cmip6Dataset dataset = CreateDataset();
        CreateInputFile(dataset, ClimateVariable.Precipitation, "pr_day_ACCESS-ESM1-5_ssp245_r1i1p1f1_gn_20410101-20501231.nc");
        VariableInfo original = dataset.GetVariableInfo(ClimateVariable.Precipitation);
        VariableInfo renamed = new("precip", original.Units);

        string fileName = dataset.GenerateOutputFileName(ClimateVariable.Precipitation, renamed);

        Assert.Equal("precip_day_ACCESS-ESM1-5_ssp245_r1i1p1f1_gn_204101-205012.nc", fileName);
    }

    [Fact]
    public void GenerateOutputFileName_ThrowsWhenNoInputFilesExist()
    {
        Cmip6Dataset dataset = CreateDataset();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            dataset.GenerateOutputFileName(ClimateVariable.Precipitation, new VariableInfo("pr", "kg m-2 s-1")));
        Assert.Contains("No input files found for variable", ex.Message);
    }

    [Fact]
    public void GenerateOutputFileName_ThrowsWhenVariantNamesAreInconsistent()
    {
        Cmip6Dataset dataset = CreateDataset();
        CreateInputFile(dataset, ClimateVariable.ShortwaveRadiation, "rsds_day_ACCESS-ESM1-5_ssp245_r1i1p1f1_gn_20410101-20501231.nc");
        CreateInputFile(dataset, ClimateVariable.ShortwaveRadiation, "rsds_day_ACCESS-ESM1-5_ssp245_r2i1p1f1_gn_20510101-20601231.nc");

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            dataset.GenerateOutputFileName(ClimateVariable.ShortwaveRadiation, dataset.GetVariableInfo(ClimateVariable.ShortwaveRadiation)));
        Assert.Contains("variant", ex.Message);
    }

    [Fact]
    public void GenerateOutputFileName_ThrowsWhenGridNamesAreInconsistent()
    {
        Cmip6Dataset dataset = CreateDataset();
        CreateInputFile(dataset, ClimateVariable.SurfacePressure, "ps_day_ACCESS-ESM1-5_ssp245_r1i1p1f1_gn_20410101-20501231.nc");
        CreateInputFile(dataset, ClimateVariable.SurfacePressure, "ps_day_ACCESS-ESM1-5_ssp245_r1i1p1f1_gr_20510101-20601231.nc");

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            dataset.GenerateOutputFileName(ClimateVariable.SurfacePressure, dataset.GetVariableInfo(ClimateVariable.SurfacePressure)));
        Assert.Contains("grid", ex.Message);
    }

    [Fact]
    public void GenerateOutputFileName_ThrowsWhenFilenameHasTooFewParts()
    {
        Cmip6Dataset dataset = CreateDataset();
        CreateInputFile(dataset, ClimateVariable.MinTemperature, "tasmin_day_ACCESS-ESM1-5_20410101-20501231.nc");

        Assert.Throws<ArgumentException>(() =>
            dataset.GenerateOutputFileName(ClimateVariable.MinTemperature, dataset.GetVariableInfo(ClimateVariable.MinTemperature)));
    }

    [Fact]
    public void GetProcessors_ReturnsExpectedStandardProcessorsForDailyOutput()
    {
        Cmip6Dataset dataset = CreateDataset();
        TestJobCreationContext context = new();
        context.MutableConfig.OutputTimeStepHours = 24;

        List<StandardVariableProcessor> processors = dataset
            .GetProcessors(context)
            .OfType<StandardVariableProcessor>()
            .ToList();

        ClimateVariable[] expected =
        [
            ClimateVariable.RelativeHumidity,
            ClimateVariable.Precipitation,
            ClimateVariable.ShortwaveRadiation,
            ClimateVariable.WindSpeed,
            ClimateVariable.SurfacePressure,
            ClimateVariable.MaxTemperature,
            ClimateVariable.MinTemperature
        ];

        Assert.Equal(expected.Length, processors.Count);
        foreach (ClimateVariable variable in expected)
            Assert.Contains(processors, p => p.TargetVariable == variable);
    }

    [Fact]
    public void GetProcessors_ThrowsWhenOutputTimeStepIsNotDaily()
    {
        Cmip6Dataset dataset = CreateDataset();
        TestJobCreationContext context = new();
        context.MutableConfig.OutputTimeStepHours = 3;

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => dataset.GetProcessors(context));
        Assert.Contains("only supports daily output", ex.Message);
    }

    private Cmip6Dataset CreateDataset(
        Cmip6Gcm gcm = Cmip6Gcm.AccessEsm15,
        Cmip6Experiment experiment = Cmip6Experiment.Ssp245)
    {
        return new Cmip6Dataset(tempDirectory.AbsolutePath, gcm, experiment);
    }

    private static void CreateInputFile(Cmip6Dataset dataset, ClimateVariable variable, string fileName)
    {
        string directory = dataset.GetInputFilesDirectory(variable);
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, fileName), string.Empty);
    }
}

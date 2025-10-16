using System.Reflection;
using ClimateProcessing.Configuration;
using ClimateProcessing.Models;
using ClimateProcessing.Services;
using ClimateProcessing.Services.Processors;
using ClimateProcessing.Tests.Helpers;
using ClimateProcessing.Tests.Mocks;
using Xunit;

namespace ClimateProcessing.Tests.Models;

public class SiloDatasetTests : IDisposable
{
    private readonly TempDirectory workingDirectory;
    private readonly string inputDirectory;
    private readonly string outputDirectory;

    public SiloDatasetTests()
    {
        workingDirectory = TempDirectory.Create();
        inputDirectory = Path.Combine(workingDirectory.AbsolutePath, "input");
        outputDirectory = Path.Combine(workingDirectory.AbsolutePath, "output");

        Directory.CreateDirectory(inputDirectory);
        Directory.CreateDirectory(outputDirectory);
    }

    public void Dispose()
    {
        workingDirectory.Dispose();
    }

    [Theory]
    [InlineData(ClimateVariable.Precipitation, "daily_rain")]
    [InlineData(ClimateVariable.MaxTemperature, "max_temp")]
    [InlineData(ClimateVariable.MinTemperature, "min_temp")]
    [InlineData(ClimateVariable.ShortwaveRadiation, "radiation")]
    [InlineData(ClimateVariable.MaxRelativeHumidity, "rh_tmax")]
    [InlineData(ClimateVariable.MinRelativeHumidity, "rh_tmin")]
    public void GetInputFilesDirectory_ReturnsExpectedPath(ClimateVariable variable, string varName)
    {
        SiloDataset dataset = CreateDataset();
        string expected = Path.Combine(inputDirectory, varName);
        Assert.Equal(expected, dataset.GetInputFilesDirectory(variable));
    }

    [Fact]
    public void GetInputFilesDirectory_ThrowsForInvalidVariable()
    {
        SiloDataset dataset = CreateDataset();
        Assert.Throws<ArgumentException>(() => dataset.GetInputFilesDirectory(ClimateVariable.Vpd));
        Assert.Throws<ArgumentException>(() => dataset.GetInputFilesDirectory(ClimateVariable.Temperature));
        Assert.Throws<ArgumentException>(() => dataset.GetInputFilesDirectory(ClimateVariable.RelativeHumidity));
        Assert.Throws<ArgumentException>(() => dataset.GetInputFilesDirectory(ClimateVariable.WindSpeed));
    }

    [Theory]
    [InlineData(ClimateVariable.Precipitation, "daily_rain")]
    [InlineData(ClimateVariable.MaxTemperature, "max_temp")]
    [InlineData(ClimateVariable.MinTemperature, "min_temp")]
    [InlineData(ClimateVariable.ShortwaveRadiation, "radiation")]
    [InlineData(ClimateVariable.MaxRelativeHumidity, "rh_tmax")]
    [InlineData(ClimateVariable.MinRelativeHumidity, "rh_tmin")]
    public void GetInputFiles_ReturnsAllNcFiles(ClimateVariable variable, string varName)
    {
        SiloDataset dataset = CreateDataset();
        using TempDirectory dir = new TempDirectory(Path.Combine(inputDirectory, varName));
        File.Create(Path.Combine(dir.AbsolutePath, $"1990.{varName}.nc")).Dispose();
        File.Create(Path.Combine(dir.AbsolutePath, $"1991.{varName}.nc")).Dispose();
        File.Create(Path.Combine(dir.AbsolutePath, $"1992.{varName}.xy")).Dispose();

        Assert.Equal(2, dataset.GetInputFiles(variable).Count());
    }

    [Theory]
    [InlineData(ClimateVariable.Precipitation, "daily_rain")]
    [InlineData(ClimateVariable.MaxTemperature, "max_temp")]
    [InlineData(ClimateVariable.MinTemperature, "min_temp")]
    [InlineData(ClimateVariable.ShortwaveRadiation, "radiation")]
    [InlineData(ClimateVariable.MaxRelativeHumidity, "rh_tmax")]
    [InlineData(ClimateVariable.MinRelativeHumidity, "rh_tmin")]
    public void GetInputFiles_ReturnsEmptyCollectionForEmptyDirectory(ClimateVariable variable, string varName)
    {
        SiloDataset dataset = CreateDataset();
        using TempDirectory dir = new TempDirectory(Path.Combine(inputDirectory, varName));
        Assert.Empty(dataset.GetInputFiles(variable));
    }

    [Fact]
    public void GetInputFiles_ThrowsForMissingDirectory()
    {
        SiloDataset dataset = CreateDataset();
        Assert.Throws<DirectoryNotFoundException>(() => dataset.GetInputFiles(ClimateVariable.Precipitation));
    }

    [Theory]
    [InlineData(ClimateVariable.Precipitation, "daily_rain", "mm")]
    [InlineData(ClimateVariable.MaxTemperature, "max_temp", "Celsius")]
    [InlineData(ClimateVariable.MinTemperature, "min_temp", "Celsius")]
    [InlineData(ClimateVariable.ShortwaveRadiation, "radiation", "Mj/m2")]
    [InlineData(ClimateVariable.MaxRelativeHumidity, "rh_tmax", "%")]
    [InlineData(ClimateVariable.MinRelativeHumidity, "rh_tmin", "%")]
    public void GetVariableInfo_ReturnsCorrectMetadata(ClimateVariable variable, string expectedName, string expectedUnits)
    {
        SiloDataset dataset = CreateDataset();
        VariableInfo info = dataset.GetVariableInfo(variable);
        Assert.Equal(expectedName, info.Name);
        Assert.Equal(expectedUnits, info.Units);
    }

    [Theory]
    [InlineData(ClimateVariable.Vpd)]
    [InlineData(ClimateVariable.Temperature)]
    [InlineData(ClimateVariable.RelativeHumidity)]
    [InlineData(ClimateVariable.SurfacePressure)]
    [InlineData(ClimateVariable.SpecificHumidity)]
    [InlineData(ClimateVariable.WindSpeed)]
    public void GetVariableInfo_ThrowsForInvalidVariable(ClimateVariable variable)
    {
        SiloDataset dataset = CreateDataset();
        Assert.Throws<ArgumentException>(() => dataset.GetVariableInfo(variable));
    }

    [Fact]
    public void GetOutputDirectory_ReturnsDot()
    {
        SiloDataset dataset = CreateDataset();
        Assert.Equal(".", dataset.GetOutputDirectory());
    }

    [Fact]
    public void GetProcessors_ReturnsExpectedProcessors()
    {
        SiloDataset dataset = CreateDataset();
        IJobCreationContext context = CreateContext();

        // Create min/max tas & rel. humidity files so we can generate output names for the mean processors
        const int nfiles = 4;
        CreateYearlyFiles(dataset, ClimateVariable.MinTemperature, nfiles);
        CreateYearlyFiles(dataset, ClimateVariable.MaxTemperature, nfiles);
        CreateYearlyFiles(dataset, ClimateVariable.MinRelativeHumidity, nfiles);
        CreateYearlyFiles(dataset, ClimateVariable.MaxRelativeHumidity, nfiles);

        IEnumerable<IVariableProcessor> processors = dataset.GetProcessors(context);

        AssertContains(processors, ClimateVariable.Precipitation);
        AssertContains(processors, ClimateVariable.ShortwaveRadiation);
        AssertContains(processors, ClimateVariable.MinTemperature);
        AssertContains(processors, ClimateVariable.MaxTemperature);
        AssertContains(processors, ClimateVariable.Temperature);
        AssertContains(processors, ClimateVariable.RelativeHumidity);
    }

    [Fact]
    public void EnsureOutputFiles_UseRenamedVariableNames()
    {
        IJobCreationContext context = CreateContext();
        SiloDataset dataset = CreateDataset();

        const int nfiles = 16;
        CreateYearlyFiles(dataset, ClimateVariable.MinTemperature, nfiles);
        CreateYearlyFiles(dataset, ClimateVariable.MaxTemperature, nfiles);
        CreateYearlyFiles(dataset, ClimateVariable.MinRelativeHumidity, nfiles);
        CreateYearlyFiles(dataset, ClimateVariable.MaxRelativeHumidity, nfiles);

        IEnumerable<IVariableProcessor> processors = dataset.GetProcessors(context);

        string tempOutFile = GetMeanProcessorOutputFileName(ClimateVariable.Temperature, processors);
        Assert.Equal("1960.1975.tas.nc", tempOutFile);

        string hursOutFile = GetMeanProcessorOutputFileName(ClimateVariable.RelativeHumidity, processors);
        Assert.Equal("1960.1975.hurs.nc", hursOutFile);
    }

    [Fact]
    public void GenerateOutputFileName_GeneratesValidFileName()
    {
        SiloDataset dataset = CreateDataset();
        const ClimateVariable variable = ClimateVariable.Precipitation;
        using TempDirectory dir = new TempDirectory(dataset.GetInputFilesDirectory(variable));
        // Create years 1960..1975 inclusive -> min 1960, max 1975
        for (int year = 1960; year <= 1975; year++)
            File.Create(Path.Combine(dir.AbsolutePath, $"{year}.daily_rain.nc")).Dispose();

        string actual = dataset.GenerateOutputFileName(variable, dataset.GetVariableInfo(variable));
        Assert.Equal("1960.1975.daily_rain.nc", actual);
    }

    [Theory]
    [InlineData(ClimateVariable.Precipitation, "precip")]
    [InlineData(ClimateVariable.ShortwaveRadiation, "rad")]
    public void GenerateOutputFileName_UsesRenamedFileName(ClimateVariable variable, string newName)
    {
        SiloDataset dataset = CreateDataset();
        VariableInfo metadata = dataset.GetVariableInfo(variable);
        VariableInfo newMetadata = new VariableInfo(newName, metadata.Units);

        using TempDirectory dir = new TempDirectory(dataset.GetInputFilesDirectory(variable));
        for (int year = 1960; year <= 1974; year++)
            File.Create(Path.Combine(dir.AbsolutePath, $"{year}.{metadata.Name}.nc")).Dispose();

        string actual = dataset.GenerateOutputFileName(variable, newMetadata);
        Assert.Equal($"1960.1974.{newName}.nc", actual);
    }

    [Fact]
    public void GenerateOutputFileName_ThrowsWithoutInputFiles()
    {
        SiloDataset dataset = CreateDataset();
        Assert.Throws<DirectoryNotFoundException>(() => dataset.GenerateOutputFileName(ClimateVariable.Precipitation, dataset.GetVariableInfo(ClimateVariable.Precipitation)));
    }

    [Fact]
    public void TestDatasetName()
    {
        SiloDataset dataset = CreateDataset();
        Assert.Equal("SILO", dataset.DatasetName);
    }

    private TempDirectory CreateYearlyFiles(IClimateDataset dataset, ClimateVariable variable, int nfiles, int baseYear = 1960)
    {
        string varName = dataset.GetVariableInfo(variable).Name;
        TempDirectory directory = new TempDirectory(dataset.GetInputFilesDirectory(variable));
        for (int i = 0; i < nfiles; i++)
        {
            int year = baseYear + i;
            File.Create(Path.Combine(directory.AbsolutePath, $"{year}.{varName}.nc")).Dispose();
        }
        return directory;
    }

    private static string GetMeanProcessorOutputFileName(ClimateVariable variable, IEnumerable<IVariableProcessor> processors)
    {
        RechunkProcessorDecorator rechunker = processors.OfType<RechunkProcessorDecorator>().First(x => x.TargetVariable == variable);
        MeanProcessor innerProcessor = GetField<MeanProcessor>(rechunker, "innerProcessor");
        return GetField<string>(innerProcessor, "outputFileName");
    }

    private static T GetField<T>(object obj, string fieldName)
    {
        FieldInfo field = obj.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        return (T)field.GetValue(obj)!;
    }

    private void AssertContains(IEnumerable<IVariableProcessor> processors, ClimateVariable variable)
    {
        Assert.Contains(processors, p => p.OutputFormat.Variable == variable && p.OutputFormat.Stage == ProcessingStage.Rechunked);
    }

    private IJobCreationContext CreateContext(ModelVersion version = ModelVersion.Trunk)
    {
        ProcessingConfig mockConfig = new TestProcessingConfig();
        mockConfig.Version = version;

        PBSConfig config = new PBSConfig("q", 1, 1, 1, "", PBSWalltime.Parse("01:00:00"), EmailNotificationType.None, "");

        return new JobCreationContext(
            mockConfig,
            new PathManager(outputDirectory),
            new InMemoryScriptWriterFactory(),
            new ClimateVariableManager(version),
            new PBSWriter(config, new PathManager(outputDirectory)),
            new PBSWriter(config, new PathManager(outputDirectory)),
            new RemappingService(),
            new DependencyResolver()
        );
    }

    private SiloDataset CreateDataset()
    {
        return new SiloDataset(inputDirectory);
    }
}

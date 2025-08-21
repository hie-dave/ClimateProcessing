using ClimateProcessing.Configuration;
using ClimateProcessing.Models;
using ClimateProcessing.Models.Cordex;
using ClimateProcessing.Services;
using ClimateProcessing.Tests.Helpers;
using ClimateProcessing.Tests.Mocks;
using Moq;
using Xunit;

namespace ClimateProcessing.Tests.Models;

public class CordexDatasetTests : IDisposable
{
    private readonly TempDirectory workingDirectory;
    private readonly string inputDirectory;
    private readonly string outputDirectory;

    public CordexDatasetTests()
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
    [InlineData(CordexActivity.DD, CordexInstitution.BOM, CordexGcm.AccessCM2, CordexExperiment.Historical, CordexSource.BarpaR, ClimateVariable.Precipitation)]
    [InlineData(CordexActivity.DD, CordexInstitution.BOM, CordexGcm.AccessCM2, CordexExperiment.Historical, CordexSource.BarpaR, ClimateVariable.ShortwaveRadiation)]
    [InlineData(CordexActivity.DD, CordexInstitution.BOM, CordexGcm.AccessCM2, CordexExperiment.Historical, CordexSource.Ccamv2203SN, ClimateVariable.WindSpeed)]
    [InlineData(CordexActivity.DD, CordexInstitution.BOM, CordexGcm.AccessCM2, CordexExperiment.Ssp126, CordexSource.Ccamv2203SN, ClimateVariable.MaxTemperature)]
    [InlineData(CordexActivity.DD, CordexInstitution.BOM, CordexGcm.AccessCM2, CordexExperiment.Ssp126, CordexSource.Ccamv2203SN, ClimateVariable.MinTemperature)]
    [InlineData(CordexActivity.DD, CordexInstitution.BOM, CordexGcm.AccessEsm15, CordexExperiment.Ssp370, CordexSource.Ccamv2203SN, ClimateVariable.MaxTemperature)]
    [InlineData(CordexActivity.DD, CordexInstitution.BOM, CordexGcm.Cesm2, CordexExperiment.Ssp370, CordexSource.Ccamv2203SN, ClimateVariable.MinTemperature)]
    [InlineData(CordexActivity.DD, CordexInstitution.CSIRO, CordexGcm.CmccEsm2, CordexExperiment.Ssp370, CordexSource.BarpaR, ClimateVariable.MaxRelativeHumidity)]
    [InlineData(CordexActivity.DD, CordexInstitution.CSIRO, CordexGcm.CmccEsm2, CordexExperiment.Ssp370, CordexSource.BarpaR, ClimateVariable.MinRelativeHumidity)]
    [InlineData(CordexActivity.BiasCorrected, CordexInstitution.CSIRO, CordexGcm.CmccEsm2, CordexExperiment.Ssp370, CordexSource.BarpaR, ClimateVariable.Precipitation)]
    [InlineData(CordexActivity.BiasCorrected, CordexInstitution.CSIRO, CordexGcm.CmccEsm2, CordexExperiment.Ssp370, CordexSource.BarpaR, ClimateVariable.ShortwaveRadiation)]
    [InlineData(CordexActivity.BiasCorrected, CordexInstitution.CSIRO, CordexGcm.CmccEsm2, CordexExperiment.Ssp370, CordexSource.BarpaR, ClimateVariable.WindSpeed)]
    public void GetInputFilesDirectory_ReturnsCorrectDirectory(
        CordexActivity activity,    
        CordexInstitution institution,
        CordexGcm gcm,
        CordexExperiment experiment,
        CordexSource source,
        ClimateVariable variable)
    {
        CordexDataset dataset = CreateDataset(activity, institution, gcm, experiment, source);
        string expected = Path.Combine(
            inputDirectory,
            "output",
            "CMIP6",
            activity.ToActivityId(),
            "AUST-05i",
            institution.ToInstitutionId(),
            gcm.ToGcmId(),
            experiment.ToExperimentId(),
            gcm.GetVariantLabel(),
            source.ToSourceId(),
            "v1-r1",
            "day",
            dataset.GetVariableInfo(variable).Name,
            "latest");
        Assert.Equal(expected, dataset.GetInputFilesDirectory(variable));
    }

    [Fact]
    public void GetInputFilesDirectory_ThrowsForInvalidVariable()
    {
        CordexDataset dataset = CreateDataset();
        Assert.Throws<ArgumentException>(() => dataset.GetInputFilesDirectory(ClimateVariable.Vpd));
    }

    [Theory]
    [InlineData(ClimateVariable.Precipitation)]
    [InlineData(ClimateVariable.ShortwaveRadiation)]
    [InlineData(ClimateVariable.WindSpeed)]
    [InlineData(ClimateVariable.MaxTemperature)]
    [InlineData(ClimateVariable.MinTemperature)]
    [InlineData(ClimateVariable.MaxRelativeHumidity)]
    [InlineData(ClimateVariable.MinRelativeHumidity)]
    public void GetInputFiles_ReturnsAllFilesInDirectory(ClimateVariable variable)
    {
        CordexDataset dataset = CreateDataset();

        // Create a few sample input files.
        using TempDirectory directory = new TempDirectory(dataset.GetInputFilesDirectory(variable));
        File.Create(Path.Combine(directory.AbsolutePath, "file1.nc")).Dispose();
        File.Create(Path.Combine(directory.AbsolutePath, "file2.nc")).Dispose();
        File.Create(Path.Combine(directory.AbsolutePath, "file2.xy")).Dispose();

        Assert.Equal(2, dataset.GetInputFiles(variable).Count());
    }

    [Theory]
    [InlineData(ClimateVariable.Precipitation)]
    [InlineData(ClimateVariable.ShortwaveRadiation)]
    [InlineData(ClimateVariable.WindSpeed)]
    [InlineData(ClimateVariable.MaxTemperature)]
    [InlineData(ClimateVariable.MinTemperature)]
    [InlineData(ClimateVariable.MaxRelativeHumidity)]
    [InlineData(ClimateVariable.MinRelativeHumidity)]
    public void GetInputFiles_ReturnsEmptyCollectionForEmptyDirectory(ClimateVariable variable)
    {
        CordexDataset dataset = CreateDataset();
        using TempDirectory directory = new TempDirectory(dataset.GetInputFilesDirectory(variable));
        Assert.Empty(dataset.GetInputFiles(variable));
    }

    [Fact]
    public void GetInputFiles_ReturnsEmptyCollectionForMissingDirectory()
    {
        CordexDataset dataset = CreateDataset();
        Assert.Empty(dataset.GetInputFiles(ClimateVariable.Precipitation));
    }

    [Fact]
    public void GetInputFiles_ThrowsForInvalidVariable()
    {
        CordexDataset dataset = CreateDataset();
        Assert.Throws<ArgumentException>(() => dataset.GetInputFiles(ClimateVariable.Vpd));
    }

    [Theory]
    [InlineData(CordexActivity.DD, CordexInstitution.BOM, CordexGcm.AccessCM2, CordexExperiment.Historical, CordexSource.BarpaR)]
    [InlineData(CordexActivity.BiasCorrected, CordexInstitution.CSIRO, CordexGcm.ECEarth3, CordexExperiment.Ssp370, CordexSource.Ccamv2203SN)]
    public void GetOutputDirectory_ReturnsCorrectDirectory(
        CordexActivity activity,
        CordexInstitution institution,
        CordexGcm gcm,
        CordexExperiment experiment,
        CordexSource source)
    {
        CordexDataset dataset = CreateDataset(activity, institution, gcm, experiment, source);
        // TODO: is it better to just check that the directory contains each
        // of these components, rather than making the test as brittle as this?
        string expected = Path.Combine(
            gcm.ToGcmId(),
            experiment.ToExperimentId(),
            activity.ToActivityId(),
            institution.ToInstitutionId(),
            source.ToSourceId());
        Assert.Equal(expected, dataset.GetOutputDirectory());
    }

    [Theory]
    [InlineData(ClimateVariable.Precipitation, "pr", "mm d-1")]
    [InlineData(ClimateVariable.ShortwaveRadiation, "rsds", "W m-2")]
    [InlineData(ClimateVariable.WindSpeed, "sfcWindmax", "m s-1")]
    [InlineData(ClimateVariable.MaxTemperature, "tasmax", "degC")]
    [InlineData(ClimateVariable.MinTemperature, "tasmin", "degC")]
    [InlineData(ClimateVariable.MaxRelativeHumidity, "hursmax", "%")]
    [InlineData(ClimateVariable.MinRelativeHumidity, "hursmin", "%")]
    public void GetVariableInfo_ReturnsCorrectMetadata(ClimateVariable variable, string expectedName, string expectedUnits)
    {
        CordexDataset dataset = CreateDataset();
        VariableInfo info = dataset.GetVariableInfo(variable);
        Assert.Equal(expectedName, info.Name);
        Assert.Equal(expectedUnits, info.Units);
    }

    [Theory]
    [InlineData(ClimateVariable.Vpd)]
    [InlineData(ClimateVariable.Temperature)]
    [InlineData(ClimateVariable.RelativeHumidity)]
    public void GetVariableInfo_ThrowsForInvalidVariable(ClimateVariable variable)
    {
        CordexDataset dataset = CreateDataset();
        Assert.Throws<ArgumentException>(() => dataset.GetVariableInfo(variable));
    }

    [Fact]
    public void GetVariableProcessors_ReturnsCorrectProcessors()
    {
        CordexDataset dataset = CreateDataset();
        IJobCreationContext context = CreateContext();

        // We need to generate a few sample input files for min/max tas and hurs
        // so that output file names can be generated by the processors.
        const int nfiles = 3;
        using TempDirectory directory = CreateTempFiles(dataset, ClimateVariable.MaxTemperature, "tasmax", nfiles);
        using TempDirectory directory2 = CreateTempFiles(dataset, ClimateVariable.MaxRelativeHumidity, "hursmax", nfiles);
        using TempDirectory directory3 = CreateTempFiles(dataset, ClimateVariable.MinTemperature, "tasmin", nfiles);
        using TempDirectory directory4 = CreateTempFiles(dataset, ClimateVariable.MinRelativeHumidity, "hursmin", nfiles);

        IEnumerable<IVariableProcessor> processors = dataset.GetProcessors(context);
        AssertContains(processors, ClimateVariable.Precipitation);
        AssertContains(processors, ClimateVariable.ShortwaveRadiation);
        AssertContains(processors, ClimateVariable.WindSpeed);
        AssertContains(processors, ClimateVariable.MinTemperature);
        AssertContains(processors, ClimateVariable.MaxTemperature);
        AssertContains(processors, ClimateVariable.Temperature);
        AssertContains(processors, ClimateVariable.RelativeHumidity);

        // Min and max relative humidity aren't required by the model, so no
        // need to test for them here. Ideally, we would stop processing them
        // after the timeseries stage anyway.
    }

    [Fact]
    public void GetVariableProcessors_ThrowsForInvalidModelVersion()
    {
        CordexDataset dataset = CreateDataset();
        IJobCreationContext context = CreateContext(ModelVersion.Dave);
        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => dataset.GetProcessors(context));
        Assert.Contains("version", ex.Message);
    }

    [Fact]
    public void GenerateOutputFileName_GeneratesValidFileName()
    {
        CordexDataset dataset = CreateDataset(gcm: CordexGcm.MpiEsm12HR);
        // Actual file name in the dataset:
        // pr_AUST-05i_MPI-ESM1-2-HR_historical_r1i1p1f1_BOM_BARPA-R_v1-r1_day_19600101-19601231.nc
        string prefix = "pr_AUST-05i_MPI-ESM1-2-HR_historical_r1i1p1f1_BOM_BARPA-R_v1-r1_day";
        ClimateVariable variable = ClimateVariable.Precipitation;
        using TempDirectory directory = CreateTempFiles(dataset, variable, prefix, 16);
        string actual = dataset.GenerateOutputFileName(variable);

        string expected = "pr_AUST-05i_MPI-ESM1-2-HR_historical_r1i1p1f1_BOM_BARPA-R_v1-r1_day_19600101-19751231.nc";
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GenerateOutputFileName_ThrowsWithoutInputFiles()
    {
        CordexDataset dataset = CreateDataset();
        Assert.Throws<InvalidOperationException>(() => dataset.GenerateOutputFileName(ClimateVariable.Precipitation));
    }

    [Theory]
    [InlineData("pr_AUST-05i_MPI-ESM1-2-HR_historical_r1i1p1f1_BOM_BARPA-R_v1-r1_day_19600101_19601231.nc")] // note: underscore separating dates - hypen is required
    [InlineData("asdf.nc")]
    [InlineData("x.nc")]
    public void GenerateOutputFileName_ThrowsForInvalidFilename(string fileName)
    {
        CordexDataset dataset = CreateDataset();

        const ClimateVariable variable = ClimateVariable.Precipitation;
        using TempDirectory directory = new TempDirectory(dataset.GetInputFilesDirectory(variable));
        File.Create(Path.Combine(directory.AbsolutePath, fileName)).Dispose();

        ArgumentException ex = Assert.Throws<ArgumentException>(() => dataset.GenerateOutputFileName(variable));
        Assert.Contains("date", ex.Message);
    }

    [Theory]
    [InlineData(CordexActivity.DD, CordexDomain.Aust05i, CordexInstitution.BOM, CordexGcm.AccessCM2, CordexExperiment.Historical, CordexSource.BarpaR)]
    [InlineData(CordexActivity.BiasCorrected, CordexDomain.Aust05i, CordexInstitution.CSIRO, CordexGcm.Cesm2, CordexExperiment.Ssp370, CordexSource.Ccamv2203SN)]
    public void DatasetName_IncludesCorrectComponents(
        CordexActivity activity,
        CordexDomain domain,
        CordexInstitution institution,
        CordexGcm gcm,
        CordexExperiment experiment,
        CordexSource source)
    {
        CordexDataset dataset = CreateDataset(activity, institution, gcm, experiment, source);
        Assert.Contains(domain.ToDomainId(), dataset.DatasetName);
        Assert.Contains(institution.ToInstitutionId(), dataset.DatasetName);
        Assert.Contains(gcm.ToGcmId(), dataset.DatasetName);
        Assert.Contains(experiment.ToExperimentId(), dataset.DatasetName);
        Assert.Contains(source.ToSourceId(), dataset.DatasetName);
    }

    private TempDirectory CreateTempFiles(IClimateDataset dataset, ClimateVariable variable, string prefix, int nfiles)
    {
        DateTime baseDate = new DateTime(1960, 1, 1);
        TempDirectory directory = new TempDirectory(dataset.GetInputFilesDirectory(variable));
        for (int i = 0; i < nfiles; i++)
        {
            DateTime startDate = baseDate.AddYears(i);
            DateTime endDate = baseDate.AddYears(i + 1).AddDays(-1);
            File.Create(Path.Combine(directory.AbsolutePath, $"{prefix}_{startDate:yyyyMMdd}-{endDate:yyyyMMdd}.nc")).Dispose();
        }
        return directory;
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

    private CordexDataset CreateDataset(
        CordexActivity activity = CordexActivity.DD,
        CordexInstitution institution = CordexInstitution.BOM,
        CordexGcm gcm = CordexGcm.AccessCM2,
        CordexExperiment experiment = CordexExperiment.Historical,
        CordexSource source = CordexSource.BarpaR)
    {
        return new CordexDataset(
            inputDirectory,
            activity,
            CordexDomain.Aust05i,
            institution,
            gcm,
            experiment,
            source);
    }
}

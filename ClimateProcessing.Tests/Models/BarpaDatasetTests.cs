using ClimateProcessing.Models;
using ClimateProcessing.Models.Barpa;
using ClimateProcessing.Services;
using ClimateProcessing.Tests.Mocks;
using Xunit;

namespace ClimateProcessing.Tests.Models;

public class BarpaDatasetTests : IDisposable
{
    private readonly TempDirectory tempDirectory = TempDirectory.Create(nameof(BarpaDatasetTests));

    public void Dispose() => tempDirectory.Dispose();

    public static TheoryData<BarpaProject, BarpaDomain, BarpaGcm, BarpaExperiment, BarpaFrequency,
        string, string, string> ArchiveConfigurations => new()
    {
        {
            BarpaProject.Output, BarpaDomain.Aust04, BarpaGcm.Era5,
            BarpaExperiment.Evaluation, BarpaFrequency.Hourly,
            "AUST-04_ERA5_evaluation_r1i1p1f1_BOM_BARPA-C_v1-r1_1hr",
            "BARPA/output/CMIP6/DD/AUST-04/BOM/ERA5/evaluation/r1i1p1f1/BARPA-C/v1-r1/1hr/tas/latest",
            "output/AUST-04/ERA5/evaluation/1hr"
        },
        {
            BarpaProject.OutputCmip6, BarpaDomain.Aus15, BarpaGcm.AccessCM2,
            BarpaExperiment.Ssp585, BarpaFrequency.Hourly,
            "AUS-15_ACCESS-CM2_ssp585_r4i1p1f1_BOM_BARPA-R_v1-r1_1hr",
            "BARPA/output-CMIP6/DD/AUS-15/BOM/ACCESS-CM2/ssp585/r4i1p1f1/BARPA-R/v1-r1/1hr/tas/latest",
            "output-CMIP6/AUS-15/ACCESS-CM2/ssp585/1hr"
        },
        {
            BarpaProject.OutputCmip6, BarpaDomain.Aust04, BarpaGcm.AccessEsm15,
            BarpaExperiment.Historical, BarpaFrequency.SixHourly,
            "AUST-04_ACCESS-ESM1-5_historical_r6i1p1f1_BOM_BARPA-C_v1-r1_6hr",
            "BARPA/output-CMIP6/DD/AUST-04/BOM/ACCESS-ESM1-5/historical/r6i1p1f1/BARPA-C/v1-r1/6hr/tas/latest",
            "output-CMIP6/AUST-04/ACCESS-ESM1-5/historical/6hr"
        },
        {
            BarpaProject.OutputCmip6, BarpaDomain.Aust15, BarpaGcm.Cesm2,
            BarpaExperiment.Ssp126, BarpaFrequency.Monthly,
            "AUST-15_CESM2_ssp126_r11i1p1f1_BOM_BARPA-R_v1-r1_mon",
            "BARPA/output-CMIP6/DD/AUST-15/BOM/CESM2/ssp126/r11i1p1f1/BARPA-R/v1-r1/mon/tas/latest",
            "output-CMIP6/AUST-15/CESM2/ssp126/mon"
        },
        {
            BarpaProject.CordexCmip6, BarpaDomain.Aus20i, BarpaGcm.ECEarth3,
            BarpaExperiment.Ssp370, BarpaFrequency.ThreeHourly,
            "AUS-20i_EC-Earth3_ssp370_r1i1p1f1_BOM_BARPA-R1-NN_v1-r1_3hr",
            "BARPA/CORDEX-CMIP6/DD/AUS-20i/BOM/EC-Earth3/ssp370/r1i1p1f1/BARPA-R1-NN/v1-r1/3hr/tas/latest",
            "CORDEX-CMIP6/AUS-20i/EC-Earth3/ssp370/3hr"
        },
        {
            BarpaProject.CordexCmip6, BarpaDomain.Aus20i, BarpaGcm.MpiEsm12HR,
            BarpaExperiment.Historical, BarpaFrequency.Daily,
            "AUS-20i_MPI-ESM1-2-HR_historical_r1i1p1f1_BOM_BARPA-R1-NN_v1-r1_day",
            "BARPA/CORDEX-CMIP6/DD/AUS-20i/BOM/MPI-ESM1-2-HR/historical/r1i1p1f1/BARPA-R1-NN/v1-r1/day/tas/latest",
            "CORDEX-CMIP6/AUS-20i/MPI-ESM1-2-HR/historical/day"
        }
    };

    [Theory]
    [MemberData(nameof(ArchiveConfigurations))]
    public void ArchiveLayout_UsesConfiguredIdentifiers(
        BarpaProject project, BarpaDomain domain, BarpaGcm gcm,
        BarpaExperiment experiment, BarpaFrequency frequency,
        string expectedName, string expectedInputDirectory, string expectedOutputDirectory)
    {
        BarpaDataset dataset = CreateDataset(project, domain, gcm, experiment, frequency);

        Assert.Equal(expectedName, dataset.DatasetName);
        Assert.Equal(Path.Combine(tempDirectory.AbsolutePath, PortablePath(expectedInputDirectory)),
            dataset.GetInputFilesDirectory(ClimateVariable.Temperature));
        Assert.Equal(PortablePath(expectedOutputDirectory), dataset.GetOutputDirectory());
        Assert.False(Path.IsPathRooted(dataset.GetOutputDirectory()));
    }

    [Theory]
    [InlineData(ClimateVariable.SpecificHumidity, "huss", "1")]
    [InlineData(ClimateVariable.RelativeHumidity, "hurs", "%")]
    [InlineData(ClimateVariable.Temperature, "tas", "K")]
    [InlineData(ClimateVariable.WindSpeed, "sfcWind", "m s-1")]
    [InlineData(ClimateVariable.Precipitation, "pr", "kg m-2 s-1")]
    [InlineData(ClimateVariable.ShortwaveRadiation, "rsds", "W m-2")]
    [InlineData(ClimateVariable.SurfacePressure, "ps", "Pa")]
    public void VariableMetadataAndDirectory_UseArchiveNameAndUnits(
        ClimateVariable variable, string expectedName, string expectedUnits)
    {
        BarpaDataset dataset = CreateDataset();

        VariableInfo info = dataset.GetVariableInfo(variable);

        Assert.Equal(expectedName, info.Name);
        Assert.Equal(expectedUnits, info.Units);
        string expectedDirectory = Path.Combine(tempDirectory.AbsolutePath,
            "BARPA", "CORDEX-CMIP6", "DD", "AUS-15", "BOM", "ACCESS-CM2",
            "historical", "r4i1p1f1", "BARPA-R1-NN", "v1-r1", "1hr", expectedName, "latest");
        Assert.Equal(expectedDirectory, dataset.GetInputFilesDirectory(variable));
    }

    [Theory]
    [InlineData(ClimateVariable.MinTemperature)]
    [InlineData(ClimateVariable.MaxTemperature)]
    [InlineData(ClimateVariable.MinRelativeHumidity)]
    [InlineData(ClimateVariable.MaxRelativeHumidity)]
    [InlineData(ClimateVariable.Vpd)]
    [InlineData((ClimateVariable)(-1))]
    public void VariableAccess_RejectsUnsupportedVariables(ClimateVariable variable)
    {
        BarpaDataset dataset = CreateDataset();

        Assert.ThrowsAny<ArgumentException>(() => dataset.GetVariableInfo(variable));
        Assert.ThrowsAny<ArgumentException>(() => dataset.GetInputFilesDirectory(variable));
        Assert.ThrowsAny<ArgumentException>(() => dataset.GetInputFiles(variable).ToList());
        Assert.ThrowsAny<ArgumentException>(() =>
            dataset.GenerateOutputFileName(variable, new VariableInfo("unsupported", "1")));
    }

    [Fact]
    public void GetInputFiles_ThrowsWhenVariableDirectoryIsMissing()
    {
        BarpaDataset dataset = CreateDataset();

        Assert.Throws<DirectoryNotFoundException>(() =>
            dataset.GetInputFiles(ClimateVariable.Temperature).ToList());
    }

    [Fact]
    public void GetInputFiles_ReturnsEmptyWhenVariableDirectoryIsEmpty()
    {
        BarpaDataset dataset = CreateDataset();
        Directory.CreateDirectory(dataset.GetInputFilesDirectory(ClimateVariable.Temperature));

        Assert.Empty(dataset.GetInputFiles(ClimateVariable.Temperature));
    }

    [Fact]
    public void GetInputFiles_ReturnsOnlyRequestedVariableNetcdfFilesInDateOrder()
    {
        BarpaDataset dataset = CreateDataset();
        string latest = CreateInputFile(dataset, ClimateVariable.Temperature,
            "200101010000-200112312300");
        string middle = CreateInputFile(dataset, ClimateVariable.Temperature,
            "200001010030-200012312330");
        string earliest = CreateInputFile(dataset, ClimateVariable.Temperature,
            "200001010000-200001010029");
        CreateInputFile(dataset, ClimateVariable.Precipitation, "199901010000-199912312300");
        string directory = dataset.GetInputFilesDirectory(ClimateVariable.Temperature);
        File.WriteAllText(Path.Combine(directory, "README.txt"), string.Empty);
        File.WriteAllText(Path.Combine(directory, "invalid.nc.bak"), string.Empty);

        Assert.Equal(new[] { earliest, middle, latest },
            dataset.GetInputFiles(ClimateVariable.Temperature));
    }

    [Theory]
    [InlineData("tas_missing_dates.nc")]
    [InlineData("tas_200001010000_200012312300.nc")]
    [InlineData("tas_2000010100000-2000123123000.nc")]
    [InlineData("tas_200013010000-200101010000.nc")]
    [InlineData("tas_200102290000-200103010000.nc")]
    [InlineData("tas_200001012400-200101010000.nc")]
    public void GetInputFiles_RejectsMalformedDateRangesOrInvalidStartDates(string fileName)
    {
        BarpaDataset dataset = CreateDataset();
        string directory = dataset.GetInputFilesDirectory(ClimateVariable.Temperature);
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, fileName), string.Empty);

        Assert.Throws<InvalidOperationException>(() =>
            dataset.GetInputFiles(ClimateVariable.Temperature).ToList());
    }

    [Theory]
    [InlineData("pr")]
    [InlineData("precip")]
    public void GenerateOutputFileName_UsesOutputMetadataAndFullInputDateRange(string outputName)
    {
        BarpaDataset dataset = CreateDataset();
        // Create files out of chronological order, with different start/end
        // timestamps, so both sorting and selection of the end date matter.
        CreateInputFile(dataset, ClimateVariable.Precipitation, "200101010000-200101010045");
        CreateInputFile(dataset, ClimateVariable.Precipitation, "199912312330-199912312359");
        CreateInputFile(dataset, ClimateVariable.Precipitation, "200001010000-200012312330");
        CreateInputFile(dataset, ClimateVariable.Temperature, "199001010000-201012312330");

        string fileName = dataset.GenerateOutputFileName(
            ClimateVariable.Precipitation, new VariableInfo(outputName, "mm"));

        Assert.Equal($"{outputName}_AUS-15_ACCESS-CM2_historical_r4i1p1f1_BOM_BARPA-R1-NN_v1-r1_1hr_199912312330-200101010045.nc",
            fileName);
    }

    [Theory]
    [InlineData("200002290015-200002292345")]
    [InlineData("199912312330-200001010045")]
    public void GenerateOutputFileName_PreservesBothTimestampsForSingleInputFile(string dateRange)
    {
        BarpaDataset dataset = CreateDataset();
        CreateInputFile(dataset, ClimateVariable.Temperature, dateRange);

        string fileName = dataset.GenerateOutputFileName(
            ClimateVariable.Temperature, new VariableInfo("tas", "K"));

        Assert.Equal($"tas_AUS-15_ACCESS-CM2_historical_r4i1p1f1_BOM_BARPA-R1-NN_v1-r1_1hr_{dateRange}.nc",
            fileName);
    }

    [Theory]
    [InlineData("200013010000")]
    [InlineData("200102290000")]
    [InlineData("200001012400")]
    public void GenerateOutputFileName_RejectsInvalidEndDates(string endDate)
    {
        BarpaDataset dataset = CreateDataset();
        CreateInputFile(dataset, ClimateVariable.Temperature, $"200001010000-{endDate}");

        Assert.Throws<InvalidOperationException>(() => dataset.GenerateOutputFileName(
            ClimateVariable.Temperature, new VariableInfo("tas", "K")));
    }

    [Fact]
    public void GenerateOutputFileName_ThrowsWhenNoNetcdfInputFilesExist()
    {
        BarpaDataset dataset = CreateDataset();
        string directory = dataset.GetInputFilesDirectory(ClimateVariable.Temperature);
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "README.txt"), string.Empty);

        Assert.Throws<InvalidOperationException>(() => dataset.GenerateOutputFileName(
            ClimateVariable.Temperature, new VariableInfo("tas", "K")));
    }

    [Theory]
    [InlineData(ModelVersion.Trunk, 24)]
    [InlineData(ModelVersion.Dave, 1)]
    [InlineData(ModelVersion.Dave, 24)]
    public void GetProcessors_ReturnsRequiredVariablesAndProcessingStages(ModelVersion version, int outputHours)
    {
        BarpaDataset dataset = CreateDataset();
        TestJobCreationContext context = new(version);
        context.MutableConfig.InputTimeStepHours = version == ModelVersion.Dave ? 1 : 24;
        context.MutableConfig.OutputTimeStepHours = outputHours;
        ClimateVariable[] standard =
        [
            ClimateVariable.Temperature,
            ClimateVariable.SpecificHumidity,
            ClimateVariable.Precipitation,
            ClimateVariable.SurfacePressure,
            ClimateVariable.ShortwaveRadiation,
            ClimateVariable.WindSpeed
        ];
        ClimateVariable[] expected = version == ModelVersion.Dave
            ? [..standard, ClimateVariable.Vpd]
            : standard;

        List<IVariableProcessor> processors = dataset.GetProcessors(context).ToList();

        Assert.Equal(expected.OrderBy(v => v), processors.Select(p => p.TargetVariable).OrderBy(v => v));
        foreach (IVariableProcessor processor in processors)
        {
            Assert.Equal(ClimateVariableFormat.Rechunked(processor.TargetVariable), processor.OutputFormat);
            Assert.Contains(ClimateVariableFormat.Timeseries(processor.TargetVariable), processor.IntermediateOutputs);
            if (processor.TargetVariable != ClimateVariable.Vpd)
                Assert.Empty(processor.Dependencies);
        }

        if (version == ModelVersion.Dave)
        {
            IVariableProcessor vpd = Assert.Single(processors, p => p.TargetVariable == ClimateVariable.Vpd);
            ClimateVariableFormat[] expectedDependencies =
            [
                ClimateVariableFormat.Timeseries(ClimateVariable.Temperature),
                ClimateVariableFormat.Timeseries(ClimateVariable.SpecificHumidity),
                ClimateVariableFormat.Timeseries(ClimateVariable.SurfacePressure)
            ];
            Assert.True(vpd.Dependencies.SetEquals(expectedDependencies));
        }
    }

    [Theory]
    [InlineData(VPDMethod.Magnus, "17.27")]
    [InlineData(VPDMethod.Buck1981, "18.678")]
    [InlineData(VPDMethod.AlduchovEskridge1996, "17.625")]
    [InlineData(VPDMethod.AllenFAO1998, "0.6108")]
    [InlineData(VPDMethod.Sonntag1990, "17.08085")]
    public async Task GetProcessors_DaveUsesConfiguredVpdMethodAndRechunksResult(
        VPDMethod method, string distinguishingCoefficient)
    {
        BarpaDataset dataset = CreateDataset();
        TestJobCreationContext context = new(ModelVersion.Dave);
        context.MutableConfig.VPDMethod = method;
        context.MockPathManager.SetOutputFileName(ClimateVariable.Temperature, "tas_example.nc");
        IVariableProcessor vpd = Assert.Single(dataset.GetProcessors(context),
            p => p.TargetVariable == ClimateVariable.Vpd);
        foreach (ClimateVariableFormat dependency in vpd.Dependencies)
            context.ConfigureDependency(dependency);

        IReadOnlyList<Job> jobs = await vpd.CreateJobsAsync(dataset, context);

        Job calculation = Assert.Single(jobs,
            j => j.Output == ClimateVariableFormat.Timeseries(ClimateVariable.Vpd));
        Job rechunk = Assert.Single(jobs,
            j => j.Output == ClimateVariableFormat.Rechunked(ClimateVariable.Vpd));
        // Check a distinguishing coefficient rather than snapshotting the script.
        Assert.Contains(distinguishingCoefficient, context.ReadScript(calculation));
        Assert.Contains(calculation, rechunk.Dependencies);
    }

    private BarpaDataset CreateDataset(
        BarpaProject project = BarpaProject.CordexCmip6,
        BarpaDomain domain = BarpaDomain.Aus15,
        BarpaGcm gcm = BarpaGcm.AccessCM2,
        BarpaExperiment experiment = BarpaExperiment.Historical,
        BarpaFrequency frequency = BarpaFrequency.Hourly)
    {
        return new BarpaDataset(tempDirectory.AbsolutePath, project, domain, gcm, experiment, frequency);
    }

    private static string CreateInputFile(BarpaDataset dataset, ClimateVariable variable, string dateRange)
    {
        string directory = dataset.GetInputFilesDirectory(variable);
        Directory.CreateDirectory(directory);
        string fileName = $"{dataset.GetVariableInfo(variable).Name}_{dataset.DatasetName}_{dateRange}.nc";
        string path = Path.Combine(directory, fileName);
        File.WriteAllText(path, string.Empty);
        return path;
    }

    private static string PortablePath(string path) => Path.Combine(path.Split('/'));
}

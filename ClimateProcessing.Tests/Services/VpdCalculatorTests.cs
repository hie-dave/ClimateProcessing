using System.Text.RegularExpressions;
using ClimateProcessing.Configuration;
using ClimateProcessing.Models;
using ClimateProcessing.Services;
using ClimateProcessing.Services.Processors;
using ClimateProcessing.Tests.Helpers;
using ClimateProcessing.Tests.Mocks;
using Moq;
using Xunit;

using static ClimateProcessing.Tests.Helpers.AssertionHelpers;

namespace ClimateProcessing.Tests.Services;

public sealed class VpdCalculatorTests : IDisposable
{
    private readonly TempDirectory outputDirectory;
    private readonly PathManager pathManager;
    private readonly InMemoryScriptWriterFactory factory;
    private readonly PBSWriter pbsWriter;

    // Job creation context initialised to return the above objects.
    private readonly Mock<IJobCreationContext> mockContext;

    public VpdCalculatorTests()
    {
        outputDirectory = TempDirectory.Create(GetType().Name);
        factory = new InMemoryScriptWriterFactory();
        pathManager = new(outputDirectory.AbsolutePath);

        // TODO: simplify PBS config.
        PBSWalltime walltime = PBSWalltime.Parse("01:00:00");
        EmailNotificationType email = EmailNotificationType.None;
        PBSConfig config = new("testq", 1, 1, 1, "", walltime, email, "");
        pbsWriter = new PBSWriter(config, pathManager);

        mockContext = new Mock<IJobCreationContext>();
        mockContext.Setup(x => x.DependencyResolver).Returns(new DependencyResolver());
        mockContext.Setup(x => x.PathManager).Returns(pathManager);
        mockContext.Setup(x => x.PBSLightweight).Returns(pbsWriter);
        mockContext.Setup(x => x.VariableManager).Returns(new ClimateVariableManager(ModelVersion.Dave));
        mockContext.Setup(x => x.FileWriterFactory).Returns(factory);
    }

    public void Dispose()
    {
        outputDirectory.Dispose();
    }

    [Theory]
    [InlineData(VPDMethod.Magnus)]
    [InlineData(VPDMethod.Buck1981)]
    [InlineData(VPDMethod.AlduchovEskridge1996)]
    [InlineData(VPDMethod.AllenFAO1998)]
    [InlineData(VPDMethod.Sonntag1990)]
    public async Task GenerateVPDScript_GeneratesValidScript(VPDMethod method)
    {
        StaticMockDataset dataset = new("/input");

        Job temperatureJob = new Job("temperature", "tas", ClimateVariableFormat.Timeseries(ClimateVariable.Temperature), "tas.nc", Array.Empty<Job>());
        Job specificHumidityJob = new Job("specificHumidity", "huss", ClimateVariableFormat.Timeseries(ClimateVariable.SpecificHumidity), "huss.nc", Array.Empty<Job>());
        Job surfacePressureJob = new Job("surfacePressure", "ps", ClimateVariableFormat.Timeseries(ClimateVariable.SurfacePressure), "ps.nc", Array.Empty<Job>());

        DependencyResolver resolver = new DependencyResolver();
        mockContext.Setup(x => x.DependencyResolver).Returns(resolver);
        resolver.AddJobs([
            temperatureJob,
            specificHumidityJob,
            surfacePressureJob
        ]);

        VpdCalculator calculator = new VpdCalculator(method);
        IReadOnlyList<Job> jobs = await calculator.CreateJobsAsync(dataset, mockContext.Object);

        Job job = Assert.Single(jobs);
        string scriptContent = factory.Read($"calc_vpd_{dataset.DatasetName}");
        AssertScriptValid(scriptContent);
    }

    [Theory]
    [InlineData(VPDMethod.Magnus, "tas", "huss", "ps")]
    [InlineData(VPDMethod.Buck1981, "x", "y", "z")]
    [InlineData(VPDMethod.AlduchovEskridge1996, "asdf", "b", "test")]
    [InlineData(VPDMethod.AllenFAO1998, "d", "e", "f")]
    [InlineData(VPDMethod.Sonntag1990, "g", "h", "i")]
    public async Task GenerateVPDScript_UsesCorrectNames(VPDMethod method, string temp, string huss, string ps)
    {
        DynamicMockDataset dataset = new("/input", outputDirectory.AbsolutePath);
        dataset.SetVariableInfo(ClimateVariable.Temperature, temp, "degC");
        dataset.SetVariableInfo(ClimateVariable.SpecificHumidity, huss, "kg kg-1");
        dataset.SetVariableInfo(ClimateVariable.SurfacePressure, ps, "Pa");

        using InMemoryScriptWriter writer = new InMemoryScriptWriter();
        VpdCalculator calculator = new VpdCalculator(method);

        await calculator.WriteVPDEquationsAsync(writer, dataset);
        string equationContent = writer.GetContent();

        // Remove comment lines.
        string sanitised = Regex.Replace(equationContent, @"#.*\n", "");

        // Ensure all lines end with a semicolon.
        Assert.DoesNotMatch(@"[^;\r]\n", sanitised);

        Assert.Matches(@"_e=.*;\n", sanitised);
        Assert.Matches(@"_esat=.*;\n", sanitised);
        Assert.Matches(@"vpd=.*;\n", sanitised);
        ValidateEquations(equationContent);
    }

    [Theory]
    [InlineData(VPDMethod.Magnus)]
    [InlineData(VPDMethod.Buck1981)]
    [InlineData(VPDMethod.AlduchovEskridge1996)]
    [InlineData(VPDMethod.AllenFAO1998)]
    [InlineData(VPDMethod.Sonntag1990)]
    public async Task GenerateVPDScript_GeneratesValidEquations(VPDMethod method)
    {
        DynamicMockDataset dataset = new("/input", outputDirectory.AbsolutePath);
        dataset.SetVariableInfo(ClimateVariable.Temperature, "tas", "degC");
        dataset.SetVariableInfo(ClimateVariable.SpecificHumidity, "huss", "kg kg-1");
        dataset.SetVariableInfo(ClimateVariable.SurfacePressure, "ps", "Pa");

        using InMemoryScriptWriter writer = new InMemoryScriptWriter();
        VpdCalculator calculator = new VpdCalculator(method);

        await calculator.WriteVPDEquationsAsync(writer, dataset);
        string equationContent = writer.GetContent();

        // Remove comment lines.
        string sanitised = Regex.Replace(equationContent, @"#.*\n", "");

        // Ensure all lines end with a semicolon.
        Assert.DoesNotMatch(@"[^;\r]\n", sanitised);

        Assert.Matches(@"_e=.*;\n", sanitised);
        Assert.Matches(@"_esat=.*;\n", sanitised);
        Assert.Matches(@"vpd=.*;\n", sanitised);
        ValidateEquations(equationContent);
    }

    [Fact]
    public async Task WriteVPDEquationsAsync_ThrowsForInvalidMethod()
    {
        using InMemoryScriptWriter writer = new InMemoryScriptWriter();
        StaticMockDataset dataset = new StaticMockDataset("x");

        VPDMethod method = (VPDMethod)666;
        VpdCalculator calculator = new VpdCalculator(method);
        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(async () => await calculator.WriteVPDEquationsAsync(writer, dataset));
        Assert.Contains("method", exception.Message);
    }

    private static void ValidateEquations(string content)
    {
        // Check for valid CDO expression syntax.
        IEnumerable<string> lines = content.Split('\n')
            // Skip comments.
            .Where(line => !line.TrimStart().StartsWith('#'))
            // Skip empty lines.
            .Where(line => !string.IsNullOrWhiteSpace(line));

        foreach (string line in lines)
        {
            // Each non-comment line should end with semicolon.
            Assert.EndsWith(";", line.Trim());

            // Basic syntax check - equal number of opening/closing parentheses.
            int openParens = line.Count(c => c == '(');
            int closeParens = line.Count(c => c == ')');
            Assert.Equal(openParens, closeParens);
        }
    }
}

using System.Text.RegularExpressions;
using ClimateProcessing.Configuration;
using ClimateProcessing.Models;
using ClimateProcessing.Services;
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
    private readonly IFileWriterFactory factory;
    private readonly PBSWriter pbsWriter;

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

        FileWriterFactory factory = new FileWriterFactory(pathManager);
        VpdCalculator calculator = new VpdCalculator(method, pathManager, factory);
        string scriptPath = await calculator.GenerateVPDScript(dataset, pbsWriter, "-O");

        Assert.True(File.Exists(scriptPath));
        string scriptContent = await File.ReadAllTextAsync(scriptPath);
        AssertScriptValid(scriptContent);
    }

    [Theory]
    [InlineData(VPDMethod.Magnus)]
    [InlineData(VPDMethod.Buck1981)]
    [InlineData(VPDMethod.AlduchovEskridge1996)]
    [InlineData(VPDMethod.AllenFAO1998)]
    [InlineData(VPDMethod.Sonntag1990)]
    public async Task GenerateVPDScript_GeneratesCorrectEquations(VPDMethod method)
    {
        StaticMockDataset dataset = new("/input");
        InMemoryScriptWriter writer = new InMemoryScriptWriter();
        VpdCalculator calculator = new VpdCalculator(method, pathManager, factory);
        await calculator.WriteVPDEquationsAsync(writer, dataset);
        string equationContent = writer.GetContent();

        // Remove comment lines.
        string sanitised = Regex.Replace(equationContent, @"#.*\n", "");

        // Ensure all lines end with a semicolon.
        Assert.DoesNotMatch(@"[^;\r]\n", sanitised);

        Assert.Matches(@"_e=.*;\n", sanitised);
        Assert.Matches(@"_esat=.*;\n", sanitised);
        Assert.Matches(@"vpd=.*;\n", sanitised);
    }
}

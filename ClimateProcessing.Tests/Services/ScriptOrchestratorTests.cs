using Xunit;
using ClimateProcessing.Services;
using ClimateProcessing.Models;
using ClimateProcessing.Tests.Mocks;
using Xunit.Abstractions;

using static ClimateProcessing.Tests.Helpers.AssertionHelpers;
using static ClimateProcessing.Tests.Helpers.ResourceHelpers;
using ClimateProcessing.Tests.Helpers;
using ClimateProcessing.Services.Processors;

namespace ClimateProcessing.Tests.Services;

public class ScriptOrchestratorTests : IDisposable
{
    private readonly ITestOutputHelper outputHelper;
    private readonly TempDirectory tempDirectory;
    private readonly NarClim2Config _config;

    public ScriptOrchestratorTests(ITestOutputHelper outputHelper)
    {
        this.outputHelper = outputHelper;
        tempDirectory = TempDirectory.Create(GetType().Name);

        _config = new NarClim2Config()
        {
            Project = "test",
            Queue = "normal",
            Walltime = "01:00:00",
            Ncpus = 1,
            Memory = 4,
            OutputDirectory = tempDirectory.AbsolutePath,
            InputTimeStepHours = 1,
            OutputTimeStepHours = 24
        };
    }

    /// <summary>
    /// Teardown method - delete the temporary output directory.
    /// </summary>
    public void Dispose()
    {
        tempDirectory.Dispose();
    }

    [Theory]
    [InlineData(true)]   // With VPD calculation
    [InlineData(false)]  // Without VPD calculation
    public async Task GenerateScriptsAsync_HandlesVPDDependenciesCorrectly(
        bool requiresVPD)
    {
        NarClim2Config config = new()
        {
            Project = "test",
            Queue = "normal",
            Walltime = "01:00:00",
            Ncpus = 1,
            Memory = 4,
            OutputDirectory = tempDirectory.AbsolutePath,
            InputDirectory = "/input",
            Version = requiresVPD ? ModelVersion.Dave : ModelVersion.Trunk,
            InputTimeStepHours = 1,
            OutputTimeStepHours = 1
        };
        ScriptOrchestrator generator = new(config);
        DynamicMockDataset dataset = new(config.InputDirectory, config.OutputDirectory);
        List<IVariableProcessor> processors = new List<IVariableProcessor>();
        processors.Add(new StandardVariableProcessor(ClimateVariable.Temperature));
        processors.Add(new StandardVariableProcessor(ClimateVariable.SpecificHumidity));
        processors.Add(new StandardVariableProcessor(ClimateVariable.SurfacePressure));
        if (requiresVPD)
            processors.Add(new VpdCalculator(VPDMethod.Magnus));
        dataset.SetProcessors(processors);

        string scriptPath = await generator.GenerateScriptsAsync(dataset);

        Assert.True(File.Exists(scriptPath));
        string scriptContent = await File.ReadAllTextAsync(scriptPath);

        // Basic script validation
        AssertScriptValid(scriptContent);

        if (requiresVPD)
        {
            // Should have all required VPD variables
            Assert.Contains("huss", scriptContent);
            Assert.Contains("ps", scriptContent);
            Assert.Contains("tas", scriptContent);
            Assert.Contains("vpd", scriptContent);

            // Should have proper dependencies
            Assert.Contains("afterok:", scriptContent);
        }
        else
        {
            // Should not have VPD-related content
            Assert.DoesNotContain("vpd", scriptContent);
        }

        // Should always have cleanup job
        Assert.Contains("cleanup_", scriptContent);
    }

    [Fact]
    public async Task TestGenerateWrapperScript()
    {
        string script = Path.GetTempFileName();
        await File.WriteAllLinesAsync(script, ["#!/usr/bin/bash", "echo x"]);
        string wrapper = await ScriptOrchestrator.GenerateWrapperScript(tempDirectory.AbsolutePath, [script]);
        string output = await File.ReadAllTextAsync(wrapper);

        // The wrapper script should call each subscript passed into it.
        Assert.Contains($"\n{script}\n", output);
    }

    [Fact]
    public async Task EnsureScriptsAreDisposedOf()
    {
        PathManager pathManager = new(_config.OutputDirectory);
        TrackingFileWriterFactory factory = new(_config.OutputDirectory);
        ScriptOrchestrator generator = new(_config, pathManager, factory, new RemappingService());
        _config.InputDirectory = "/input";
        DynamicMockDataset dataset = new(_config.InputDirectory, _config.OutputDirectory);

        await generator.GenerateScriptsAsync(dataset);

        if (factory.ActiveWriters.Count > 0)
            outputHelper.WriteLine($"Script generator failed to dispose of {factory.ActiveWriters.Count} file writers: {string.Join(", ", factory.ActiveWriters.Select(f => Path.GetFileName(f)))}");
        Assert.Empty(factory.ActiveWriters);
        Assert.True(factory.TotalWritersCreated > 0);
    }

    /// <summary>
    /// Do a full integration test of the script generator under controlled
    /// conditions and do a full string comparison against the expected output.
    /// </summary>
    /// <remarks>
    /// This will be brittle, but thorough. If this fails, the other tests can
    /// be used to figure out why. If no other tests fail, then we need more
    /// tests!
    /// </remarks>
    [Fact]
    public async Task GenerateScriptsAsync_IntegrationTest()
    {
        const VPDMethod method = VPDMethod.AlduchovEskridge1996;

        const string inputDirectory = "/input";
        DynamicMockDataset dataset = new(inputDirectory, tempDirectory.AbsolutePath);
        dataset.SetProcessors([
            new StandardVariableProcessor(ClimateVariable.SpecificHumidity),
            new StandardVariableProcessor(ClimateVariable.Precipitation),
            new StandardVariableProcessor(ClimateVariable.SurfacePressure),
            new StandardVariableProcessor(ClimateVariable.ShortwaveRadiation),
            new StandardVariableProcessor(ClimateVariable.WindSpeed),
            new StandardVariableProcessor(ClimateVariable.Temperature),
            new RechunkProcessorDecorator(new VpdCalculator(method))
        ]);
        NarClim2Config config = new()
        {
            Project = "test",
            Queue = "megamem",
            Walltime = "06:30:00",
            Ncpus = 2,
            Memory = 64,
            InputDirectory = inputDirectory,
            OutputDirectory = tempDirectory.AbsolutePath,
            InputTimeStepHours = 1,
            OutputTimeStepHours = 3,
            Version = ModelVersion.Dave,
            ChunkSizeTime = 8192,
            ChunkSizeSpatial = 24,
            GridFile = "/home/giraffe/grid.nc",
            CompressionLevel = 8,
            CompressOutput = true,
            DryRun = true,
            Email = "test@example.com",
            JobFS = 128,
            VPDMethod = method,
            EmailNotifications = EmailNotificationType.After | EmailNotificationType.Before | EmailNotificationType.Aborted
        };
        ScriptOrchestrator generator = new(config);

        // Act.
        await generator.GenerateScriptsAsync(dataset);

        // Assert.
        AssertEmptyDirectory(Path.Combine(tempDirectory.AbsolutePath, "logs"));
        AssertEmptyDirectory(Path.Combine(tempDirectory.AbsolutePath, "streams"));
        AssertEmptyDirectory(Path.Combine(tempDirectory.AbsolutePath, "output", dataset.GetOutputDirectory()));
        AssertEmptyDirectory(Path.Combine(tempDirectory.AbsolutePath, "tmp", dataset.GetOutputDirectory()));

        string scriptsDirectory = Path.Combine(tempDirectory.AbsolutePath, "scripts");
        Assert.True(Directory.Exists(scriptsDirectory));
        Assert.NotEmpty(Directory.EnumerateFileSystemEntries(scriptsDirectory));

        string[] expectedScriptNames = [
            "calc_vpd_DynamicMockDataset",
            "cleanup_DynamicMockDataset",
            "mergetime_huss_DynamicMockDataset",
            "mergetime_pr_DynamicMockDataset",
            "mergetime_ps_DynamicMockDataset",
            "mergetime_rsds_DynamicMockDataset",
            "mergetime_sfcWind_DynamicMockDataset",
            "mergetime_tas_DynamicMockDataset",
            "rechunk_huss_DynamicMockDataset",
            "rechunk_pr_DynamicMockDataset",
            "rechunk_ps_DynamicMockDataset",
            "rechunk_rsds_DynamicMockDataset",
            "rechunk_sfcWind_DynamicMockDataset",
            "rechunk_tas_DynamicMockDataset",
            "rechunk_vpd_DynamicMockDataset",
            "submit_DynamicMockDataset"
        ];

        Assert.Equal(expectedScriptNames.Count(), Directory.EnumerateFileSystemEntries(scriptsDirectory).Count());

        // Name of the directory containing this test's data files.
        const string resourcePrefix = "GenerateScriptsAsync_IntegrationTest";

        foreach (string scriptName in expectedScriptNames)
        {
            string actualScriptPath = Path.Combine(scriptsDirectory, scriptName);
            Assert.True(File.Exists(actualScriptPath), $"Script {actualScriptPath} does not exist.");
            string actualScript = await File.ReadAllTextAsync(actualScriptPath);

            // Read expected script from resource in assembly.
            string expectedScript = await ReadResourceAsync($"{resourcePrefix}.{scriptName}");
            expectedScript = expectedScript.Replace("@#OUTPUT_DIRECTORY#@", tempDirectory.AbsolutePath);

            // No custom error messages in xunit, apparently.
            if (expectedScript != actualScript)
                outputHelper.WriteLine($"Script {scriptName} is invalid");

            Assert.Equal(expectedScript, actualScript);
        }
    }
}

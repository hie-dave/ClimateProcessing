using CommandLine;
using ClimateProcessing.Models;
using ClimateProcessing.Services;
using System.Runtime.CompilerServices;
using ClimateProcessing.Configuration;

[assembly: InternalsVisibleTo("ClimateProcessing.Tests")]

// Parse command line arguments
var result = Parser.Default.ParseArguments<NarClim2Config, CordexConfig>(args);

await result.WithParsedAsync(async config =>
{
    try
    {
        await Process((ProcessingConfig)config);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error: {ex.Message}");
        Environment.Exit(1);
    }
});

static async Task Process(ProcessingConfig config)
{
    config.Validate();

    // Generate scripts.
    ScriptOrchestrator generator = new(config);
    List<string> scripts = new List<string>();
    foreach (IClimateDataset dataset in config.CreateDatasets())
    {
        string submissionScript = await generator.GenerateScriptsAsync(dataset);
        scripts.Add(submissionScript);
    }

    string wrapper = await ScriptOrchestrator.GenerateWrapperScript(config.OutputDirectory, scripts);
    Console.WriteLine($"Processing scripts have been generated in:");
    Console.WriteLine($"{Path.GetDirectoryName(config.OutputDirectory)}");
    Console.WriteLine("\nTo submit the jobs to PBS, run:");
    Console.WriteLine($"{wrapper}");
}

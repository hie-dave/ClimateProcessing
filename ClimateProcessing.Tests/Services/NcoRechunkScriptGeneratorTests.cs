using ClimateProcessing.Services;
using ClimateProcessing.Tests.Helpers;
using ClimateProcessing.Models;
using Xunit;
using Moq;
using ClimateProcessing.Tests.Mocks;

namespace ClimateProcessing.Tests.Services;

public class NcoRechunkScriptGeneratorTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(9)]
    public async Task GenerateVariableRechunkScript_ConditionallyDisablesCompression(
        int compressionLevel
    )
    {
        MutableRechunkOptions options = new MutableRechunkOptions();
        options.CompressionLevel = compressionLevel;

        using InMemoryScriptWriter writer = new InMemoryScriptWriter();
        NcoRechunkScriptGenerator generator = new NcoRechunkScriptGenerator();
        await generator.WriteRechunkScriptAsync(writer, options);

        string scriptContent = writer.GetContent();
        IEnumerable<string> ncpdqLines = scriptContent.Split('\n').Where(l => l.Contains("ncpdq"));

        if (compressionLevel > 0)
            Assert.All(ncpdqLines, l => Assert.Contains($"-L{compressionLevel}", l));
        else
            Assert.All(ncpdqLines, l => Assert.DoesNotContain("-L", l));
    }

    [Theory]
    [InlineData("tasmin", "std_min_temp", "Minimum temperature")]
    [InlineData("tasmax", "std_max_temp", "Maximum temperature")]
    public async Task CreateJobsAsync_SetsStandardName(string varName, string stdName, string longName)
    {
        MutableRechunkOptions opts = new MutableRechunkOptions();
        opts.VariableName = varName;
        opts.Metadata = new VariableMetadata(stdName, longName);

        using InMemoryScriptWriter writer = new InMemoryScriptWriter();
        NcoRechunkScriptGenerator processor = new NcoRechunkScriptGenerator();
        await processor.WriteRechunkScriptAsync(writer, opts);

        string scriptContent = writer.GetContent();
        Assert.Contains($"-a 'standard_name,{varName},o,c,{stdName}'", scriptContent);
        Assert.Contains($"-a 'long_name,{varName},o,c,{longName}'", scriptContent);
    }
}

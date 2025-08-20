using ClimateProcessing.Services;
using ClimateProcessing.Tests.Helpers;
using Xunit;

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
}

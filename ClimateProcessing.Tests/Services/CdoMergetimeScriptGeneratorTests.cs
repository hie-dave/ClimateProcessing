using ClimateProcessing.Models;
using ClimateProcessing.Services;
using ClimateProcessing.Tests.Helpers;
using ClimateProcessing.Tests.Mocks;
using ClimateProcessing.Units;
using Xunit;

namespace ClimateProcessing.Tests.Services;

public class CdoMergetimeScriptGeneratorTests
{
    private readonly CdoMergetimeScriptGenerator generator = new();

    [Theory]
    [InlineData("K", "K", "temp", false, false)]  // No conversion needed
    [InlineData("K", "degC", "temp", true, true)]  // Conversion needed
    [InlineData("W/m2", "W m-2", "rad", false, true)]  // Only renaming needed
    public void GenerateUnitConversionOperators_GeneratesCorrectOperators(
        string inputUnits,
        string targetUnits,
        string outputVar,
        bool expectsConversion,
        bool expectsRenaming)
    {
        IEnumerable<string> operators =
            CdoMergetimeScriptGenerator.GenerateUnitConversionOperators(
                outputVar,
                inputUnits,
                targetUnits,
                TimeStep.Hourly
            ).ToList();

        if (expectsConversion)
            Assert.Contains(operators, op => op.StartsWith("-subc"));
        if (expectsRenaming)
            Assert.Contains(operators, op => op.StartsWith("-setattribute"));
        if (!expectsConversion && !expectsRenaming)
            Assert.Empty(operators);
    }

    [Theory]
    [InlineData(1, 24, AggregationMethod.Mean, "-daymean,24")]   // Hourly to daily
    [InlineData(24, 24, AggregationMethod.Mean, "")] // Daily to daily
    [InlineData(3, 12, AggregationMethod.Mean, "-timselmean,4")]   // 3-hourly to 12-hourly
    [InlineData(3, 12, AggregationMethod.Sum, "-timselsum,4")]   // 3-hourly to 12-hourly
    [InlineData(3, 24, AggregationMethod.Sum, "-daysum,8")]   // 3-hourly to daily
    [InlineData(1, 12, AggregationMethod.Minimum, "-timselmin,12")]   // Hourly to 12-hourly
    [InlineData(12, 24, AggregationMethod.Maximum, "-daymax,2")]   // 12-hourly to daily
    public void GenerateTimeAggregationOperators_GeneratesCorrectOperators(
        int inputHours,
        int outputHours,
        AggregationMethod aggregationMethod,
        string expectedOperator)
    {
        TimeStep inputTimestep = new TimeStep(inputHours);
        TimeStep outputTimestep = new TimeStep(outputHours);

        string @operator = CdoMergetimeScriptGenerator.GenerateTimeAggregationOperator(
            inputTimestep,
            outputTimestep,
            aggregationMethod);

        Assert.Equal(expectedOperator, @operator);
    }

    [Theory]
    [InlineData("tas", "tas", false)]      // Same name
    [InlineData("temp", "tas", true)]      // Different name
    [InlineData("pr", "prec", true)]       // Different name
    public void GenerateVariableRenameCommand_DetectsProcessingNeeds(
        string inputVar,
        string outputVar,
        bool requiresProcessing)
    {
        string op = CdoMergetimeScriptGenerator.GenerateRenameOperator(inputVar, outputVar);
        Assert.Equal(requiresProcessing, !string.IsNullOrEmpty(op));
    }

    [Theory]
    [InlineData(InterpolationAlgorithm.Bilinear, "remapbil")]
    [InlineData(InterpolationAlgorithm.Conservative, "remapcon")]
    public async Task GetRemapOperator_ReturnsCorrectOperator(
        InterpolationAlgorithm algorithm,
        string expectedOperator)
    {
        MutableMergetimeOptions options = new();
        options.GridFile = "grid.file";
        options.RemapAlgorithm = algorithm;

        using InMemoryScriptWriter writer = new InMemoryScriptWriter();
        await generator.WriteMergetimeScriptAsync(writer, options);

        string script = writer.GetContent();
        // Note: best not to assume that the raw file name is passed as the
        // operator, because the command generator could decide to use a
        // variable instead, which is perfectly fine.
        string expected = $"-{expectedOperator},";
        Assert.Contains(expected, script);
    }

    [Fact]
    public async Task NoGridFile_DoesNotRemap()
    {
        MutableMergetimeOptions options = new();
        options.GridFile = null;
        options.RemapAlgorithm = InterpolationAlgorithm.Bilinear;

        using InMemoryScriptWriter writer = new InMemoryScriptWriter();
        await generator.WriteMergetimeScriptAsync(writer, options);

        string script = writer.GetContent();
        Assert.DoesNotContain("remap", script);
    }
}

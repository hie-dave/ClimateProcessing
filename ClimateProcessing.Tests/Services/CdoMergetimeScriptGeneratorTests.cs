using ClimateProcessing.Models;
using ClimateProcessing.Services;
using ClimateProcessing.Tests.Helpers;
using ClimateProcessing.Tests.Mocks;
using ClimateProcessing.Units;
using Moq;
using Xunit;

using static ClimateProcessing.Tests.Helpers.AssertionHelpers;

namespace ClimateProcessing.Tests.Services;

public class CdoMergetimeScriptGeneratorTests
{
    private static readonly string[] cdoTemporalAggregationOperators = [
        "daymin",
        "daymax",
        "daysum",
        "daymean",
        "dayrange",
        "dayavg",
        "daystd",
        "daystd1",
        "dayvar",
        "dayvar1",
        "timselmin",
        "timselmax",
        "timselsum",
        "timselmean",
        "timselrange",
        "timselavg",
        "timselstd",
        "timselstd1",
        "timselvar",
        "timselvar1"
    ];

    private static readonly string[] cdoArithmeticOperators = [
        "addc",
        "subc",
        "mulc",
        "divc",
        "minc",
        "maxc",
        "expr",
    ];

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

    [Theory]
    [InlineData("input dir with spaces")]
    [InlineData("/path/with/special/$chars")]
    [InlineData("/normal/path")]
    public async Task GenerateVariableMergeScript_QuotesVariablesSafely(
        string inputDir)
    {
        MutableMergetimeOptions options = new();
        options.InputDirectory = inputDir;

        using InMemoryScriptWriter writer = new InMemoryScriptWriter();
        await generator.WriteMergetimeScriptAsync(writer, options);
        string scriptContent = writer.GetContent();

        // No unquoted variable references
        AssertScriptValid(scriptContent);
    }

    [Theory]
    [InlineData(ClimateVariable.Temperature, 1, 24, "tas", "K", "-daymean,24", "-subc,273.15", "-setattribute,'tas@units=degC")]
    [InlineData(ClimateVariable.Precipitation, 1, 24, "pr", "kg m-2 s-1", "-daysum,24", "-mulc,3600", "-setattribute,'pr@units=mm'")]
    [InlineData(ClimateVariable.Precipitation, 1, 8, "pr", "kg m-2 s-1", "-timselsum,8", "-mulc,3600", "-setattribute,'pr@units=mm'")]
    [InlineData(ClimateVariable.SpecificHumidity, 1, 1, "huss", "1")] // No unit conversion or aggregation
    [InlineData(ClimateVariable.SpecificHumidity, 1, 1, "huss", "kg/kg", null, null, "-setattribute,'huss@units=1'")] // Unit rename, but no unit conversion or aggregation
    [InlineData(ClimateVariable.ShortwaveRadiation, 1, 3, "rsds", "W m-2", "-timselmean,3")] // Aggregation but no unit conversion (intensive variable)
    [InlineData(ClimateVariable.Precipitation, 1, 12, "pr", "mm", "-timselsum,12")] // Aggregation but no unit conversion (extensive variable)
    [InlineData(ClimateVariable.SurfacePressure, 1, 1, "ps", "kPa", null, "-mulc,1000", "-setattribute,'ps@units=Pa'")] // Unit conversion but no aggregation
    public async Task GenerateVariableMergeScript_GeneratesValidCDORemapCommand(
        ClimateVariable variable,
        int inputTimestepHours,
        int outputTimestepHours,
        string varName,
        string inputUnits,
        string? temporalAggregationOperator = null,
        string? unitConversionOperator = null,
        string? unitRenameOperator = null)
    {
        ClimateVariableManager manager = new ClimateVariableManager(ModelVersion.Dave);

        MutableMergetimeOptions options = new MutableMergetimeOptions();
        options.InputMetadata = new VariableInfo(varName, inputUnits);
        options.TargetMetadata = manager.GetOutputRequirements(variable);
        options.InputTimeStep = new TimeStep(inputTimestepHours);
        options.OutputTimeStep = new TimeStep(outputTimestepHours);
        options.AggregationMethod = manager.GetAggregationMethod(variable);

        using InMemoryScriptWriter writer = new InMemoryScriptWriter();
        await generator.WriteMergetimeScriptAsync(writer, options);
        string scriptContent = writer.GetContent();

        // E.g.
        // cdo -L -O -v -z zip1 -daymean,24 -subc,273.15 -setattribute,'tas@units=degC' -unpack  \"${FILE}\" \"${REMAP_DIR}/$(basename \"${FILE}\")\"
        // "    cdo -L -O -v -z zip1 -daysum,24 -mulc,3600 -setattribute,'pr@units=mm' -unpack  \"${FILE}\" \"${REMAP_DIR}/$(basename \"${FILE}\")\""
        string line = scriptContent.Split("\n").First(l => l.Contains("cdo -"));

        // CDO command should have proper structure

        // Should use -L for thread-safety.
        Assert.Contains("-L", line);

        // Should use -v for progress tracking.
        Assert.Contains("-v", line);

        // Should use -O to overwrite any existing output file.
        Assert.Contains("-O", line);

        // Should use -z zip1 for efficiency.
        Assert.Contains("-z zip1", line);

        // Should unpack data.
        Assert.Contains("-unpack", line);

        // Should apply operators (or not).
        int previousOperatorIndex = -1;
        if (temporalAggregationOperator is null)
            foreach (string @operator in cdoTemporalAggregationOperators)
                Assert.DoesNotContain(@operator, line);
        else
        {
            Assert.Contains(temporalAggregationOperator, line);
            int operatorIndex = line.IndexOf(temporalAggregationOperator);
            Assert.True(operatorIndex > previousOperatorIndex, $"Operator '{temporalAggregationOperator}' appears out of order in CDO command.");
            previousOperatorIndex = operatorIndex;
        }

        if (unitConversionOperator is null)
            foreach (string @operator in cdoArithmeticOperators)
                Assert.DoesNotContain(@operator, line);
        else
        {
            Assert.Contains(unitConversionOperator, line);
            int operatorIndex = line.IndexOf(unitConversionOperator);
            Assert.True(operatorIndex > previousOperatorIndex, $"Operator '{unitConversionOperator}' appears out of order in CDO command.");
            previousOperatorIndex = operatorIndex;
        }

        if (unitRenameOperator is null)
            Assert.DoesNotContain("setattribute", line);
        else
        {
            Assert.Contains(unitRenameOperator, line);
            int operatorIndex = line.IndexOf(unitRenameOperator);
            Assert.True(operatorIndex > previousOperatorIndex, $"Operator '{unitRenameOperator}' appears out of order in CDO command.");
            previousOperatorIndex = operatorIndex;
        }

        // TODO: assert that no additional arguments are present.

        // Rest of script should be valid.
        AssertScriptValid(scriptContent);
    }

    [Theory]
    [InlineData("precip", "pr")]
    public async Task GenerateVariableMergeScript_GeneratesValidRenameCommand(
        string inputName,
        string expectedOutputName)
    {
        const string units = "xyz";
        MutableMergetimeOptions options = new MutableMergetimeOptions();
        options.InputMetadata = new VariableInfo(inputName, units);
        options.TargetMetadata = new VariableInfo(expectedOutputName, units);

        using InMemoryScriptWriter writer = new InMemoryScriptWriter();
        await generator.WriteMergetimeScriptAsync(writer, options);
        string scriptContent = writer.GetContent();

        Assert.Contains($"-chname,'{inputName}','{expectedOutputName}'", scriptContent);
    }
}

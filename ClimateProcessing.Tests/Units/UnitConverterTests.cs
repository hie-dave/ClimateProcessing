using Xunit;
using ClimateProcessing.Units;

namespace ClimateProcessing.Tests.Units;

public class UnitConverterTests
{
    [Theory]
    [InlineData("W/m2", "W/m2", false, false)]  // Exact match
    [InlineData("W/m2", "W m-2", false, true)]  // Different notation, same meaning
    [InlineData("K", "degC", true, true)]       // Requires conversion
    [InlineData("kg m-2 s-1", "mm", true, true)] // Requires conversion
    public void AnalyzeConversion_HandlesVariousUnitCombinations(
        string inputUnits,
        string targetUnits,
        bool expectedRequiresConversion,
        bool expectedRequiresRenaming)
    {
        var result = UnitConverter.AnalyseConversion(inputUnits, targetUnits);

        Assert.Equal(expectedRequiresConversion, result.RequiresConversion);
        Assert.Equal(expectedRequiresRenaming, result.RequiresRenaming);

        if (expectedRequiresConversion)
        {
            Assert.NotNull(result.ConversionExpression);
        }
    }

    [Theory]
    [InlineData("kg / m2", "kg / m2"/*, "kg/m2"*/)] // TODO: support whitespace
    [InlineData("kg.m-2", "kg.m-2"/*, "kg/m2"*/)] // TODO: support periods
    [InlineData("kgm^-2", "kgm^-2"/*, "kg m-2"*/)]    // TODO: support carets
    [InlineData("W/m2", "W/m2"/*, "W m-2"*/)]           // TODO: support slashes
    public void TestNormalise(string input, string expected)
    {
        string actual = UnitConverter.NormaliseUnits(input);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("asdf", "asdf", true)]  // Identical
    [InlineData("Kelvin", "K", true)]  // First unit known, second unit known (equivalent)
    [InlineData("K", "degC", false)]    // First unit known, second unit known (not equivalent)
    [InlineData("degC", "asdf", false)] // First unit known, second unit unknown
    [InlineData("fdsa", "mm", false)]   // First unit unknown, second unit known
    [InlineData("jkl", "asdf", false)]  // First unit unknown, second unit unknown
    [InlineData("kg m-2", "mm", true)]  // Synonymous
    public void TestAreUnitsEquivalent(string units0, string units1, bool equivalent)
    {
        // Check commutativity as well.
        Assert.Equal(equivalent, UnitConverter.AreUnitsEquivalent(units0, units1));
        Assert.Equal(equivalent, UnitConverter.AreUnitsEquivalent(units1, units0));
    }

    [Theory]
    [InlineData("K", "degC", 1, "-subc,273.15")]
    [InlineData("kg m-2 s-1", "mm", 24, "-mulc,86400")] // Daily accumulation
    [InlineData("kg m-2 s-1", "mm", 3, "-mulc,10800")]  // 3-hourly accumulation
    [InlineData("kg m-2 s-1", "mm", 1, "-mulc,3600")]   // Hourly accumulation
    [InlineData("mm", "mm", 6, "")] // No conversion required
    [InlineData("kPa", "Pa", 1, "-mulc,1000")]
    [InlineData("degC", "K", 1, "-addc,273.15")]
    [InlineData("Celsius", "K", 1, "-addc,273.15")]
    [InlineData("mm d-1", "mm", 1, "-divc,24")] // mm/day -> mm/hr: divide by 24
    [InlineData("mm d-1", "mm", 3, "-divc,8")] // mm/day -> mm/3hr: divide by 8
    [InlineData("%", "1", 1, "-divc,100")]
    [InlineData("Mj/m2", "W m-2", 24, "-divc,0.0864")] // Mj/day -> W: divide by 0.0864
    [InlineData("Mj/m2", "W m-2", 1, "-divc,0.0036")] // Mj/hr -> W: divide by 0.0036
    public void GenerateConversionExpression_GeneratesCorrectExpressions(
        string inputUnits,
        string targetUnits,
        int timestep,
        string expectedExpression)
    {
        TimeStep timeStep = new TimeStep(timestep);
        string expression = UnitConverter.GenerateConversionExpression(inputUnits, targetUnits, timeStep);
        Assert.Equal(expectedExpression, expression);
    }

    [Theory]
    [InlineData("J m-2", "W m-2")]      // Invalid conversion
    [InlineData("unknown", "W m-2")]     // Unknown unit
    public void AnalyzeConversion_ThrowsForUnsupportedConversion(
        string inputUnits,
        string targetUnits
    )
    {
        Assert.Throws<ArgumentException>(() =>
            UnitConverter.AnalyseConversion(inputUnits, targetUnits));
    }

    [Theory]
    [InlineData(0)]    // Too small
    [InlineData(25)]   // Too large
    [InlineData(7)]    // Doesn't divide 24 evenly
    public void TimeStep_ThrowsForInvalidHours(int hours)
    {
        Assert.Throws<ArgumentException>(() => new TimeStep(hours));
    }

    [Theory]
    [InlineData(1, 3600)]       // Hourly
    [InlineData(3, 10800)]      // 3-hourly
    [InlineData(24, 86400)]     // Daily
    public void TimeStep_GetSecondsInPeriod_ReturnsCorrectValue(int hours, int expectedSeconds)
    {
        var timeStep = new TimeStep(hours);
        Assert.Equal(expectedSeconds, timeStep.GetSecondsInPeriod());
    }

    [Fact]
    public void TimeStep_StaticProperties_HaveCorrectValues()
    {
        Assert.Equal(1, TimeStep.Hourly.Hours);
        Assert.Equal(24, TimeStep.Daily.Hours);
    }
}

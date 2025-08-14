using ClimateProcessing.Models;
using ClimateProcessing.Services;
using Xunit;

namespace ClimateProcessing.Tests.Services;

public sealed class RemappingServiceTests
{
    private readonly RemappingService service = new();

    [Theory]
    [InlineData("kg m-2 s-1", true)]  // Standard notation
    [InlineData("kg/m2/s", true)]     // Division notation
    [InlineData("kg.m-2.s-1", true)]  // Dot notation
    [InlineData("kg m^-2 s^-1", true)] // Caret notation
    [InlineData("KG M-2 S-1", true)]   // Case insensitive
    [InlineData("kg  m-2  s-1", true)] // Extra spaces
    [InlineData("kgm-2s-1", true)]     // No separators
    [InlineData("W", false)]           // No per-area units
    [InlineData("kg s-1", false)]      // Time only, no area
    [InlineData("", false)]            // Empty string
    [InlineData("m2", false)]          // Area but not per-area
    [InlineData("kg/s/m2", true)]      // Different order
    public void HasPerAreaUnits_DetectsUnitsCorrectly(string units, bool expectedResult)
    {
        // Now we can test HasPerAreaUnits directly since it's internal
        var result = service.HasPerAreaUnits(units);
        Assert.Equal(expectedResult, result);
    }

    [Theory]
    [InlineData(ClimateVariable.Temperature)]
    [InlineData(ClimateVariable.SpecificHumidity)]
    public void GetRemapAlgorithm_NonAreaSensitiveVariables_AlwaysReturnsBilinear(ClimateVariable variable)
    {
        var info = new VariableInfo(Enum.GetName(variable)!, "any_units");
        var result = service.GetInterpolationAlgorithm(info, variable);

        Assert.Equal(InterpolationAlgorithm.Bilinear, result);
    }

    [Theory]
    [InlineData(ClimateVariable.Precipitation)]
    [InlineData(ClimateVariable.ShortwaveRadiation)]
    public void GetRemapAlgorithm_AreaSensitiveVariables_DependsOnUnits(ClimateVariable variable)
    {
        // Should use conservative remapping when not per-area
        var nonAreaInfo = new VariableInfo(Enum.GetName(variable)!, "W");
        var nonAreaResult = service.GetInterpolationAlgorithm(nonAreaInfo, variable);
        Assert.Equal(InterpolationAlgorithm.Conservative, nonAreaResult);

        // Should use bilinear remapping when per-area
        var areaInfo = new VariableInfo(Enum.GetName(variable)!, "W m-2");
        var areaResult = service.GetInterpolationAlgorithm(areaInfo, variable);
        Assert.Equal(InterpolationAlgorithm.Bilinear, areaResult);
    }
}

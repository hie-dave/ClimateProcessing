using ClimateProcessing.Models;
using ClimateProcessing.Services;
using ClimateProcessing.Units;
using Xunit;

namespace ClimateProcessing.Tests.Services;

public class ClimateVariableManagerTests
{
    [Theory]
    [InlineData(ClimateVariable.Temperature, "K", ModelVersion.Trunk)]
    [InlineData(ClimateVariable.Temperature, "degC", ModelVersion.Dave)]
    [InlineData(ClimateVariable.MaxTemperature, "K", ModelVersion.Trunk)]
    [InlineData(ClimateVariable.MinTemperature, "K", ModelVersion.Trunk)]
    [InlineData(ClimateVariable.Precipitation, "mm", ModelVersion.Trunk)]
    [InlineData(ClimateVariable.Precipitation, "mm", ModelVersion.Dave)]
    [InlineData(ClimateVariable.ShortwaveRadiation, "W m-2", ModelVersion.Trunk)]
    [InlineData(ClimateVariable.ShortwaveRadiation, "W m-2", ModelVersion.Dave)]
    [InlineData(ClimateVariable.SpecificHumidity, "1", ModelVersion.Trunk)]
    [InlineData(ClimateVariable.SpecificHumidity, "1", ModelVersion.Dave)]
    [InlineData(ClimateVariable.SurfacePressure, "Pa", ModelVersion.Trunk)]
    [InlineData(ClimateVariable.SurfacePressure, "Pa", ModelVersion.Dave)]
    [InlineData(ClimateVariable.WindSpeed, "m s-1", ModelVersion.Trunk)]
    [InlineData(ClimateVariable.WindSpeed, "m s-1", ModelVersion.Dave)]
    public void TestGetTargetUnits_ValidVariable(
        ClimateVariable variable,
        string expectedUnits,
        ModelVersion version)
    {
        ClimateVariableManager manager = new ClimateVariableManager(version);
        VariableInfo requirements = manager.GetOutputRequirements(variable);
        Assert.Equal(expectedUnits, requirements.Units);
    }

    [Theory]
    [InlineData(ClimateVariable.Temperature, AggregationMethod.Mean)]
    [InlineData(ClimateVariable.MaxTemperature, AggregationMethod.Maximum)]
    [InlineData(ClimateVariable.MinTemperature, AggregationMethod.Minimum)]
    [InlineData(ClimateVariable.Precipitation, AggregationMethod.Sum)]
    [InlineData(ClimateVariable.ShortwaveRadiation, AggregationMethod.Mean)]
    [InlineData(ClimateVariable.SpecificHumidity, AggregationMethod.Mean)]
    [InlineData(ClimateVariable.SurfacePressure, AggregationMethod.Mean)]
    [InlineData(ClimateVariable.WindSpeed, AggregationMethod.Mean)]
    public void TestGetTargetAggregationMethod_ValidVariable(
        ClimateVariable variable,
        AggregationMethod expectedMethod)
    {
        // Test all model versions.
        foreach (ModelVersion version in Enum.GetValues<ModelVersion>())
        {
            ClimateVariableManager manager = new ClimateVariableManager(version);
            AggregationMethod actualMethod = manager.GetAggregationMethod(variable);
            Assert.Equal(expectedMethod, actualMethod);
        }
    }

    [Theory]
    [InlineData(ModelVersion.Trunk, (ClimateVariable)1001)]
    [InlineData(ModelVersion.Dave, (ClimateVariable)1001)]
    [InlineData(ModelVersion.Dave, ClimateVariable.MaxTemperature)]
    [InlineData(ModelVersion.Dave, ClimateVariable.MinTemperature)]
    public void GetOutputRequirements_ThrowsForInvalidVariable(ModelVersion version, ClimateVariable variable)
    {
        ClimateVariableManager manager = new ClimateVariableManager(version);
        ArgumentException ex = Assert.Throws<ArgumentException>(() => manager.GetOutputRequirements(variable));
        Assert.Contains("variable", ex.Message);
    }

    [Theory]
    [InlineData(ModelVersion.Trunk)]
    [InlineData(ModelVersion.Dave)]
    public void GetAggregationMethod_DoesNotThrowForValidVariable(ModelVersion version)
    {
        ClimateVariableManager manager = new ClimateVariableManager(version);
        foreach (ClimateVariable variable in Enum.GetValues<ClimateVariable>())
        {
            Exception ex = Record.Exception(() => manager.GetAggregationMethod(variable));
            Assert.Null(ex);
        }
    }

    [Fact]
    public void GetAggregationMethod_ThrowsForInvalidVariable()
    {
        ClimateVariableManager manager = new ClimateVariableManager(ModelVersion.Trunk);
        ArgumentException ex = Assert.Throws<ArgumentException>(() => manager.GetAggregationMethod((ClimateVariable)1001));
        Assert.Contains("variable", ex.Message);
    }

    [Theory]
    [InlineData(ModelVersion.Trunk)]
    public void GetRequirements_DoesNotThrowForValidVariable(ModelVersion version)
    {
        ClimateVariableManager manager = new ClimateVariableManager(version);
        foreach (ClimateVariable variable in Enum.GetValues<ClimateVariable>())
        {
            // Trunk accepts all (current) variables except VPD.
            if (version == ModelVersion.Trunk && variable == ClimateVariable.Vpd)
                continue;

            Exception ex = Record.Exception(() => manager.GetOutputRequirements(variable));
            Assert.Null(ex);
        }
    }

    [Theory]
    [InlineData(ClimateVariable.Temperature)]
    [InlineData(ClimateVariable.Precipitation)]
    [InlineData(ClimateVariable.ShortwaveRadiation)]
    [InlineData(ClimateVariable.SpecificHumidity)]
    [InlineData(ClimateVariable.SurfacePressure)]
    [InlineData(ClimateVariable.WindSpeed)]
    public void GetDaveRequirements_DoesNotThrowForValidVariable(ClimateVariable variable)
    {
        ClimateVariableManager manager = new ClimateVariableManager(ModelVersion.Dave);
        Exception ex = Record.Exception(() => manager.GetOutputRequirements(variable));
        Assert.Null(ex);
    }
}

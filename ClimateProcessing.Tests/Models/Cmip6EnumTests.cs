using ClimateProcessing.Models.Cmip6;
using Xunit;

namespace ClimateProcessing.Tests.Models;

public class Cmip6EnumTests
{
    [Theory]
    [InlineData(Cmip6Gcm.AccessEsm15, "ACCESS-ESM1-5")]
    [InlineData(Cmip6Gcm.NorEsm2MM, "NorESM2-MM")]
    [InlineData(Cmip6Gcm.ECEarth3, "EC-Earth3")]
    public void Cmip6Gcm_ToString_ReturnsExpectedValue(Cmip6Gcm gcm, string expected)
    {
        Assert.Equal(expected, Cmip6GcmExtensions.ToString(gcm));
    }

    [Fact]
    public void Cmip6Gcm_ToString_ThrowsForInvalidValue()
    {
        Assert.Throws<ArgumentException>(() => Cmip6GcmExtensions.ToString((Cmip6Gcm)999));
    }

    [Theory]
    [InlineData("ACCESS-ESM1-5", Cmip6Gcm.AccessEsm15)]
    [InlineData("NorESM2-MM", Cmip6Gcm.NorEsm2MM)]
    [InlineData("EC-Earth3", Cmip6Gcm.ECEarth3)]
    public void Cmip6Gcm_FromString_ReturnsExpectedValue(string gcm, Cmip6Gcm expected)
    {
        Assert.Equal(expected, Cmip6GcmExtensions.FromString(gcm));
    }

    [Theory]
    [InlineData("ACCESS-ESM1-5 ")]
    [InlineData("access-esm1-5")]
    [InlineData("")]
    public void Cmip6Gcm_FromString_ThrowsForInvalidValue(string gcm)
    {
        Assert.Throws<ArgumentException>(() => Cmip6GcmExtensions.FromString(gcm));
    }

    [Theory]
    [InlineData(Cmip6Experiment.Ssp119, "ssp119")]
    [InlineData(Cmip6Experiment.Ssp245, "ssp245")]
    public void Cmip6Experiment_ToString_ReturnsExpectedValue(Cmip6Experiment experiment, string expected)
    {
        Assert.Equal(expected, Cmip6ExperimentExtensions.ToString(experiment));
    }

    [Fact]
    public void Cmip6Experiment_ToString_ThrowsForInvalidValue()
    {
        Assert.Throws<ArgumentException>(() => Cmip6ExperimentExtensions.ToString((Cmip6Experiment)999));
    }

    [Theory]
    [InlineData("ssp119", Cmip6Experiment.Ssp119)]
    [InlineData("ssp245", Cmip6Experiment.Ssp245)]
    public void Cmip6Experiment_FromString_ReturnsExpectedValue(string experiment, Cmip6Experiment expected)
    {
        Assert.Equal(expected, Cmip6ExperimentExtensions.FromString(experiment));
    }

    [Theory]
    [InlineData("SSP245")]
    [InlineData("ssp370")]
    [InlineData("")]
    public void Cmip6Experiment_FromString_ThrowsForInvalidValue(string experiment)
    {
        Assert.Throws<ArgumentException>(() => Cmip6ExperimentExtensions.FromString(experiment));
    }
}

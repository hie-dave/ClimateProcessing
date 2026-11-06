using ClimateProcessing.Models;
using ClimateProcessing.Models.Barpa;
using Xunit;

namespace ClimateProcessing.Tests.Models;

public class BarpaEnumTests
{
    [Theory]
    [InlineData(BarpaProject.Output, "output")]
    [InlineData(BarpaProject.OutputCmip6, "output-CMIP6")]
    [InlineData(BarpaProject.CordexCmip6, "CORDEX-CMIP6")]
    public void ProjectIdentifiers_ConvertInBothDirections(BarpaProject project, string identifier)
    {
        Assert.Equal(identifier, project.ToProjectId());
        Assert.Equal(project, BarpaProjectExtensions.FromString(identifier));
    }

    [Theory]
    [InlineData(BarpaDomain.Aus15, "AUS-15")]
    [InlineData(BarpaDomain.Aust04, "AUST-04")]
    [InlineData(BarpaDomain.Aust15, "AUST-15")]
    [InlineData(BarpaDomain.Aus20i, "AUS-20i")]
    public void DomainIdentifiers_ConvertInBothDirections(BarpaDomain domain, string identifier)
    {
        Assert.Equal(identifier, domain.ToDomainId());
        Assert.Equal(domain, BarpaDomainExtensions.FromString(identifier));
    }

    [Theory]
    [InlineData(BarpaGcm.Era5, "ERA5")]
    [InlineData(BarpaGcm.AccessCM2, "ACCESS-CM2")]
    [InlineData(BarpaGcm.AccessEsm15, "ACCESS-ESM1-5")]
    [InlineData(BarpaGcm.NorEsm2MM, "NorESM2-MM")]
    [InlineData(BarpaGcm.ECEarth3, "EC-Earth3")]
    [InlineData(BarpaGcm.Cesm2, "CESM2")]
    [InlineData(BarpaGcm.CmccEsm2, "CMCC-ESM2")]
    [InlineData(BarpaGcm.MpiEsm12HR, "MPI-ESM1-2-HR")]
    public void GcmIdentifiers_ConvertInBothDirections(BarpaGcm gcm, string identifier)
    {
        Assert.Equal(identifier, gcm.ToGcmId());
        Assert.Equal(gcm, BarpaGcmExtensions.FromString(identifier));
    }

    [Theory]
    [InlineData(BarpaExperiment.Historical, "historical")]
    [InlineData(BarpaExperiment.Ssp126, "ssp126")]
    [InlineData(BarpaExperiment.Ssp370, "ssp370")]
    [InlineData(BarpaExperiment.Ssp585, "ssp585")]
    [InlineData(BarpaExperiment.Evaluation, "evaluation")]
    public void ExperimentIdentifiers_ConvertInBothDirections(BarpaExperiment experiment, string identifier)
    {
        Assert.Equal(identifier, experiment.ToExperimentId());
        Assert.Equal(experiment, BarpaExperimentExtensions.FromString(identifier));
    }

    [Theory]
    [InlineData(BarpaFrequency.Hourly, "1hr")]
    [InlineData(BarpaFrequency.ThreeHourly, "3hr")]
    [InlineData(BarpaFrequency.SixHourly, "6hr")]
    [InlineData(BarpaFrequency.Daily, "day")]
    [InlineData(BarpaFrequency.Monthly, "mon")]
    public void FrequencyIdentifiers_ConvertInBothDirections(BarpaFrequency frequency, string identifier)
    {
        Assert.Equal(identifier, frequency.ToFrequencyId());
        Assert.Equal(frequency, BarpaFrequencyExtensions.FromString(identifier));
    }

    [Theory]
    [InlineData(BarpaGcm.AccessCM2, "r4i1p1f1")]
    [InlineData(BarpaGcm.AccessEsm15, "r6i1p1f1")]
    [InlineData(BarpaGcm.Cesm2, "r11i1p1f1")]
    [InlineData(BarpaGcm.CmccEsm2, "r1i1p1f1")]
    [InlineData(BarpaGcm.ECEarth3, "r1i1p1f1")]
    [InlineData(BarpaGcm.MpiEsm12HR, "r1i1p1f1")]
    [InlineData(BarpaGcm.NorEsm2MM, "r1i1p1f1")]
    [InlineData(BarpaGcm.Era5, "r1i1p1f1")]
    public void GetVariantLabel_ReturnsEnsembleMember(BarpaGcm gcm, string expected)
    {
        Assert.Equal(expected, gcm.GetVariantLabel());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("unknown")]
    public void FromString_RejectsUnknownIdentifiers(string? identifier)
    {
        Assert.ThrowsAny<ArgumentException>(() => BarpaProjectExtensions.FromString(identifier!));
        Assert.ThrowsAny<ArgumentException>(() => BarpaDomainExtensions.FromString(identifier!));
        Assert.ThrowsAny<ArgumentException>(() => BarpaGcmExtensions.FromString(identifier!));
        Assert.ThrowsAny<ArgumentException>(() => BarpaExperimentExtensions.FromString(identifier!));
        Assert.ThrowsAny<ArgumentException>(() => BarpaFrequencyExtensions.FromString(identifier!));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    public void IdentifierConversions_RejectUndefinedEnumValues(int value)
    {
        Assert.ThrowsAny<ArgumentException>(() => ((BarpaProject)value).ToProjectId());
        Assert.ThrowsAny<ArgumentException>(() => ((BarpaDomain)value).ToDomainId());
        Assert.ThrowsAny<ArgumentException>(() => ((BarpaGcm)value).ToGcmId());
        Assert.ThrowsAny<ArgumentException>(() => ((BarpaExperiment)value).ToExperimentId());
        Assert.ThrowsAny<ArgumentException>(() => ((BarpaFrequency)value).ToFrequencyId());
        Assert.ThrowsAny<ArgumentException>(() => ((BarpaGcm)value).GetVariantLabel());
    }
}

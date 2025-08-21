using ClimateProcessing.Models.Cordex;
using Xunit;

namespace ClimateProcessing.Tests.Models;

public class CordexEnumTests
{
    [Fact]
    public void CordexActivity_ToActivityId_ReturnsCorrectValues()
    {
        Assert.Equal("DD", CordexActivity.DD.ToActivityId());
        Assert.Equal("bias-adjusted-output", CordexActivity.BiasCorrected.ToActivityId());
    }

    [Fact]
    public void CordexActivity_ToActivityId_ThrowsOnInvalidValue()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CordexActivityExtensions.ToActivityId((CordexActivity)273));
    }

    [Theory]
    [InlineData("DD", CordexActivity.DD)]
    [InlineData("bias-adjusted-output", CordexActivity.BiasCorrected)]
    public void CordexActivity_FromString_ReturnsCorrectEnum(string activityId, CordexActivity expected)
    {
        Assert.Equal(expected, CordexActivityExtensions.FromString(activityId));
    }

    [Fact]
    public void CordexActivity_FromString_ThrowsOnInvalidValue()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CordexActivityExtensions.FromString("invalid"));
    }

    [Theory]
    [InlineData(CordexDomain.Aust05i, "AUST-05i")]
    public void CordexDomain_ToDomainId_ReturnsCorrectValues(CordexDomain domain, string expected)
    {
        Assert.Equal(expected, domain.ToDomainId());
    }

    [Fact]
    public void CordexDomain_ToDomainId_ThrowsOnInvalidValue()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CordexDomainExtensions.ToDomainId((CordexDomain)1334));
    }

    [Theory]
    [InlineData(CordexEra.CMIP6, "CMIP6")]
    public void CordexEra_ToEraId_ReturnsCorrectValues(CordexEra era, string expected)
    {
        Assert.Equal(expected, era.ToEraId());
    }

    [Fact]
    public void CordexEra_ToEraId_ThrowsOnInvalidValue()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CordexEraExtensions.ToEraId((CordexEra)1321));
    }

    [Theory]
    [InlineData(CordexExperiment.Historical, "historical")]
    [InlineData(CordexExperiment.Ssp126, "ssp126")]
    [InlineData(CordexExperiment.Ssp370, "ssp370")]
    public void CordexExperiment_ToExperimentId_ReturnsCorrectValues(CordexExperiment experiment, string expected)
    {
        Assert.Equal(expected, experiment.ToExperimentId());
    }

    [Fact]
    public void CordexExperiment_ToExperimentId_ThrowsOnInvalidValue()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CordexExperimentExtensions.ToExperimentId((CordexExperiment)4321));
    }

    [Theory]
    [InlineData("historical", CordexExperiment.Historical)]
    [InlineData("ssp126", CordexExperiment.Ssp126)]
    [InlineData("ssp370", CordexExperiment.Ssp370)]
    public void CordexExperiment_FromString_ReturnsCorrectEnum(string experimentId, CordexExperiment expected)
    {
        Assert.Equal(expected, CordexExperimentExtensions.FromString(experimentId));
    }

    [Fact]
    public void CordexExperiment_FromString_ThrowsOnInvalidValue()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CordexExperimentExtensions.FromString("invalid"));
    }

    [Theory]
    [InlineData(CordexFrequency.Daily, "day")]
    public void CordexFrequency_ToFrequencyId_ReturnsCorrectValues(CordexFrequency frequency, string expected)
    {
        Assert.Equal(expected, frequency.ToFrequencyId());
    }

    [Fact]
    public void CordexFrequency_ToFrequencyId_ThrowsOnInvalidValue()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CordexFrequencyExtensions.ToFrequencyId((CordexFrequency)999));
    }

    [Theory]
    [InlineData(CordexGcm.AccessCM2, "ACCESS-ESM2")]
    [InlineData(CordexGcm.AccessEsm15, "ACCESS-ESM1-5")]
    [InlineData(CordexGcm.Cesm2, "CESM2")]
    [InlineData(CordexGcm.CmccEsm2, "CMCC-ESM2")]
    [InlineData(CordexGcm.ECEarth3, "EC-Earth3")]
    [InlineData(CordexGcm.MpiEsm12HR, "MPI-ESM1-2-HR")]
    [InlineData(CordexGcm.NorEsm2MM, "NorESM2-MM")]
    public void CordexGcm_ToGcmId_ReturnsCorrectValues(CordexGcm gcm, string expected)
    {
        Assert.Equal(expected, gcm.ToGcmId());
    }

    [Fact]
    public void CordexGcm_ToGcmId_ThrowsOnInvalidValue()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CordexGcmExtensions.ToGcmId((CordexGcm)666));
    }

    [Theory]
    [InlineData("ACCESS-ESM2", CordexGcm.AccessCM2)]
    [InlineData("ACCESS-ESM1-5", CordexGcm.AccessEsm15)]
    [InlineData("CESM2", CordexGcm.Cesm2)]
    [InlineData("CMCC-ESM2", CordexGcm.CmccEsm2)]
    [InlineData("EC-Earth3", CordexGcm.ECEarth3)]
    [InlineData("MPI-ESM1-2-HR", CordexGcm.MpiEsm12HR)]
    [InlineData("NorESM2-MM", CordexGcm.NorEsm2MM)]
    public void CordexGcm_FromString_ReturnsCorrectEnum(string gcmId, CordexGcm expected)
    {
        Assert.Equal(expected, CordexGcmExtensions.FromString(gcmId));
    }

    [Fact]
    public void CordexGcm_FromString_ThrowsOnInvalidValue()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CordexGcmExtensions.FromString("invalid"));
    }

    [Theory]
    [InlineData(CordexGcm.AccessCM2, "r4i1p1f1")]
    [InlineData(CordexGcm.AccessEsm15, "r6i1p1f1")]
    [InlineData(CordexGcm.Cesm2, "r11i1p1f1")]
    [InlineData(CordexGcm.CmccEsm2, "r1i1p1f1")]
    [InlineData(CordexGcm.ECEarth3, "r1i1p1f1")]
    [InlineData(CordexGcm.MpiEsm12HR, "r1i1p1f1")]
    [InlineData(CordexGcm.NorEsm2MM, "r1i1p1f1")]
    public void CordexGcm_GetVariantLabel_ReturnsCorrectValues(CordexGcm gcm, string expected)
    {
        Assert.Equal(expected, gcm.GetVariantLabel());
    }

    [Fact]
    public void CordexGcm_GetVariantLabel_ThrowsOnInvalidValue()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CordexGcmExtensions.GetVariantLabel((CordexGcm)666));
    }

    [Theory]
    [InlineData(CordexInstitution.BOM, "BOM")]
    [InlineData(CordexInstitution.CSIRO, "CSIRO")]
    public void CordexInstitution_ToInstitutionId_ReturnsCorrectValues(CordexInstitution institution, string expected)
    {
        Assert.Equal(expected, institution.ToInstitutionId());
    }

    [Fact]
    public void CordexInstitution_ToInstitutionId_ThrowsOnInvalidValue()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CordexInstitutionExtensions.ToInstitutionId((CordexInstitution)999));
    }

    [Theory]
    [InlineData("BOM", CordexInstitution.BOM)]
    [InlineData("CSIRO", CordexInstitution.CSIRO)]
    public void CordexInstitution_FromString_ReturnsCorrectEnum(string institutionId, CordexInstitution expected)
    {
        Assert.Equal(expected, CordexInstitutionExtensions.FromString(institutionId));
    }

    [Fact]
    public void CordexInstitution_FromString_ThrowsOnInvalidValue()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CordexInstitutionExtensions.FromString("invalid"));
    }

    [Theory]
    [InlineData(CordexSource.BarpaR, "BARPA-R")]
    [InlineData(CordexSource.Ccamv2203SN, "CCAM-v2203-SN")]
    public void CordexSource_ToSourceId_ReturnsCorrectValues(CordexSource source, string expected)
    {
        Assert.Equal(expected, source.ToSourceId());
    }

    [Fact]
    public void CordexSource_ToSourceId_ThrowsOnInvalidValue()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CordexSourceExtensions.ToSourceId((CordexSource)333));
    }

    [Theory]
    [InlineData("BARPA-R", CordexSource.BarpaR)]
    [InlineData("CCAM-v2203-SN", CordexSource.Ccamv2203SN)]
    public void CordexSource_FromString_ReturnsCorrectEnum(string sourceId, CordexSource expected)
    {
        Assert.Equal(expected, CordexSourceExtensions.FromString(sourceId));
    }

    [Fact]
    public void CordexSource_FromString_ThrowsOnInvalidValue()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CordexSourceExtensions.FromString("invalid"));
    }
}

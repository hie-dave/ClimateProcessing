using ClimateProcessing.Models;
using ClimateProcessing.Models.Barpa;
using Xunit;

namespace ClimateProcessing.Tests.Models;

public class BarpaAvailabilityTests
{
    // Project/domain pairs which actually occur in the archive. Other
    // compatibility checks assume that the domain has already been validated.
    public static TheoryData<BarpaProject, BarpaDomain> ArchiveDomains => new()
    {
        { BarpaProject.Output, BarpaDomain.Aust04 },
        { BarpaProject.OutputCmip6, BarpaDomain.Aus15 },
        { BarpaProject.OutputCmip6, BarpaDomain.Aust04 },
        { BarpaProject.OutputCmip6, BarpaDomain.Aust15 },
        { BarpaProject.CordexCmip6, BarpaDomain.Aus20i }
    };

    [Theory]
    [InlineData(BarpaProject.Output, BarpaDomain.Aus15, false)]
    [InlineData(BarpaProject.Output, BarpaDomain.Aust04, true)]
    [InlineData(BarpaProject.Output, BarpaDomain.Aust15, false)]
    [InlineData(BarpaProject.Output, BarpaDomain.Aus20i, false)]
    [InlineData(BarpaProject.OutputCmip6, BarpaDomain.Aus15, true)]
    [InlineData(BarpaProject.OutputCmip6, BarpaDomain.Aust04, true)]
    [InlineData(BarpaProject.OutputCmip6, BarpaDomain.Aust15, true)]
    [InlineData(BarpaProject.OutputCmip6, BarpaDomain.Aus20i, false)]
    [InlineData(BarpaProject.CordexCmip6, BarpaDomain.Aus15, false)]
    [InlineData(BarpaProject.CordexCmip6, BarpaDomain.Aust04, false)]
    [InlineData(BarpaProject.CordexCmip6, BarpaDomain.Aust15, false)]
    [InlineData(BarpaProject.CordexCmip6, BarpaDomain.Aus20i, true)]
    public void DomainIsValidFor_MatchesArchiveAvailability(
        BarpaProject project, BarpaDomain domain, bool expected)
    {
        Assert.Equal(expected, domain.IsValidFor(project));
    }

    [Theory]
    [InlineData((BarpaProject)999)]
    [InlineData((BarpaProject)1337)]
    public void DomainIsValidFor_ThrowsForUnknownProject(BarpaProject project)
    {
        foreach (BarpaDomain domain in Enum.GetValues<BarpaDomain>())
            Assert.Throws<ArgumentOutOfRangeException>(() => domain.IsValidFor(project));
    }

    [Theory]
    [InlineData(BarpaProject.Output, BarpaDomain.Aust04, "BARPA-C")]
    [InlineData(BarpaProject.OutputCmip6, BarpaDomain.Aus15, "BARPA-R")]
    [InlineData(BarpaProject.OutputCmip6, BarpaDomain.Aust04, "BARPA-C")]
    [InlineData(BarpaProject.OutputCmip6, BarpaDomain.Aust15, "BARPA-R")]
    [InlineData(BarpaProject.CordexCmip6, BarpaDomain.Aus20i, "BARPA-R1-NN")]
    public void GetSource_SelectsRegionalOrConvectionPermittingModel(
        BarpaProject project, BarpaDomain domain, string expected)
    {
        Assert.Equal(expected, domain.GetSource(project));
    }

    [Theory]
    [InlineData(BarpaProject.Output, BarpaFrequency.Hourly, true)]
    [InlineData(BarpaProject.Output, BarpaFrequency.ThreeHourly, true)]
    [InlineData(BarpaProject.Output, BarpaFrequency.SixHourly, true)]
    [InlineData(BarpaProject.Output, BarpaFrequency.Daily, true)]
    [InlineData(BarpaProject.Output, BarpaFrequency.Monthly, true)]
    [InlineData(BarpaProject.OutputCmip6, BarpaFrequency.Hourly, true)]
    [InlineData(BarpaProject.OutputCmip6, BarpaFrequency.ThreeHourly, false)]
    [InlineData(BarpaProject.OutputCmip6, BarpaFrequency.SixHourly, true)]
    [InlineData(BarpaProject.OutputCmip6, BarpaFrequency.Daily, true)]
    [InlineData(BarpaProject.OutputCmip6, BarpaFrequency.Monthly, true)]
    [InlineData(BarpaProject.CordexCmip6, BarpaFrequency.Hourly, true)]
    [InlineData(BarpaProject.CordexCmip6, BarpaFrequency.ThreeHourly, false)]
    [InlineData(BarpaProject.CordexCmip6, BarpaFrequency.SixHourly, false)]
    [InlineData(BarpaProject.CordexCmip6, BarpaFrequency.Daily, true)]
    [InlineData(BarpaProject.CordexCmip6, BarpaFrequency.Monthly, true)]
    public void FrequencyIsValidFor_MatchesArchiveAvailability(
        BarpaProject project, BarpaFrequency frequency, bool expected)
    {
        Assert.Equal(expected, frequency.IsValidFor(project));
    }

    public static IEnumerable<object[]> GcmAvailability()
    {
        foreach (object[] archiveDomain in ArchiveDomains)
        {
            BarpaProject project = (BarpaProject)archiveDomain[0];
            BarpaDomain domain = (BarpaDomain)archiveDomain[1];
            BarpaGcm[] supported = (project, domain) switch
            {
                (BarpaProject.Output, _) => [BarpaGcm.Era5],
                (BarpaProject.OutputCmip6, BarpaDomain.Aust04) =>
                    [BarpaGcm.Era5, BarpaGcm.AccessEsm15, BarpaGcm.ECEarth3],
                _ => Enum.GetValues<BarpaGcm>()
            };

            foreach (BarpaGcm gcm in Enum.GetValues<BarpaGcm>())
                yield return [project, domain, gcm, supported.Contains(gcm)];
        }
    }

    [Theory]
    [MemberData(nameof(GcmAvailability))]
    public void GcmIsValidFor_MatchesArchiveAvailability(
        BarpaProject project, BarpaDomain domain, BarpaGcm gcm, bool expected)
    {
        Assert.Equal(expected, gcm.IsValidFor(project, domain));
    }

    [Theory]
    [MemberData(nameof(ArchiveDomains))]
    public void ExperimentIsValidFor_Era5RequiresEvaluation(BarpaProject project, BarpaDomain domain)
    {
        foreach (BarpaExperiment experiment in Enum.GetValues<BarpaExperiment>())
        {
            Assert.Equal(experiment == BarpaExperiment.Evaluation,
                experiment.IsValidFor(BarpaGcm.Era5, project, domain));
        }
    }

    public static IEnumerable<object[]> ClimateModelExperiments()
    {
        foreach (BarpaGcm gcm in Enum.GetValues<BarpaGcm>().Where(g => g != BarpaGcm.Era5))
        {
            foreach (BarpaExperiment experiment in Enum.GetValues<BarpaExperiment>())
            {
                yield return [gcm, experiment];
            }
        }
    }

    [Theory]
    [MemberData(nameof(ClimateModelExperiments))]
    public void ExperimentIsValidFor_OutputDoesNotSupportClimateModelExperiments(
        BarpaGcm gcm, BarpaExperiment experiment)
    {
        Assert.False(experiment.IsValidFor(gcm, BarpaProject.Output, BarpaDomain.Aust04));
    }

    [Theory]
    [MemberData(nameof(ClimateModelExperiments))]
    public void ExperimentIsValidFor_RegionalDomainsSupportScenariosAndRestrictSsp585(
        BarpaGcm gcm, BarpaExperiment experiment)
    {
        BarpaExperiment[] supported = gcm == BarpaGcm.AccessCM2 || gcm == BarpaGcm.ECEarth3
            ? [BarpaExperiment.Historical, BarpaExperiment.Ssp126, BarpaExperiment.Ssp370, BarpaExperiment.Ssp585]
            : [BarpaExperiment.Historical, BarpaExperiment.Ssp126, BarpaExperiment.Ssp370];
        bool expected = supported.Contains(experiment);

        Assert.Equal(expected, experiment.IsValidFor(gcm, BarpaProject.CordexCmip6, BarpaDomain.Aus20i));
        Assert.Equal(expected, experiment.IsValidFor(gcm, BarpaProject.OutputCmip6, BarpaDomain.Aus15));
        Assert.Equal(expected, experiment.IsValidFor(gcm, BarpaProject.OutputCmip6, BarpaDomain.Aust15));
    }

    [Theory]
    [MemberData(nameof(ClimateModelExperiments))]
    public void ExperimentIsValidFor_ConvectionPermittingDomainOnlySupportsHistoricalAndSsp370(
        BarpaGcm gcm, BarpaExperiment experiment)
    {
        // Model availability is checked separately by BarpaGcm.IsValidFor.
        BarpaExperiment[] supported = [BarpaExperiment.Historical, BarpaExperiment.Ssp370];

        Assert.Equal(supported.Contains(experiment),
            experiment.IsValidFor(gcm, BarpaProject.OutputCmip6, BarpaDomain.Aust04));
    }
}

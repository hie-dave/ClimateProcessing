using ClimateProcessing.Configuration;
using ClimateProcessing.Models;
using Xunit;

namespace ClimateProcessing.Tests.Models;

public class SiloConfigTests
{
    [Fact]
    public void CreateDatasets_ReturnsSingleSiloDataset()
    {
        using TempDirectory tempDirectory = TempDirectory.Create();
        SiloConfig config = new SiloConfig
        {
            InputDirectory = tempDirectory.AbsolutePath
        };

        IClimateDataset dataset = Assert.Single(config.CreateDatasets());
        Assert.IsType<SiloDataset>(dataset);
    }
}

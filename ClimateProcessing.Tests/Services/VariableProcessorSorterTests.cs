using ClimateProcessing.Models;
using ClimateProcessing.Services;
using Moq;
using Xunit;

namespace ClimateProcessing.Tests.Services;

public class VariableProcessorSorterTests
{
    private readonly VariableProcessorSorter sorter = new VariableProcessorSorter();

    [Fact]
    public void SortByDependencies_WithNoProcessors_ReturnsEmptyCollection()
    {
        IEnumerable<IVariableProcessor> result = sorter.SortByDependencies([]);
        Assert.Empty(result);
    }

    [Fact]
    public void SortByDependencies_WithNoDependencies_ReturnsSameProcessors()
    {
        Mock<IVariableProcessor> processor1 = CreateProcessor(ClimateVariable.Temperature, []);
        Mock<IVariableProcessor> processor2 = CreateProcessor(ClimateVariable.Precipitation, []);
        IEnumerable<IVariableProcessor> processors = [processor1.Object, processor2.Object];

        IEnumerable<IVariableProcessor> result = sorter.SortByDependencies(processors);

        Assert.Equal(2, result.Count());
        Assert.Contains(processor1.Object, result);
        Assert.Contains(processor2.Object, result);
    }

    [Fact]
    public void SortByDependencies_WithSimpleDependency_ReturnsCorrectOrder()
    {
        ClimateVariableFormat tempFormat = ClimateVariableFormat.Rechunked(ClimateVariable.Temperature);
        ClimateVariableFormat vpdFormat = ClimateVariableFormat.Rechunked(ClimateVariable.Vpd);

        Mock<IVariableProcessor> tempProcessor = CreateProcessor(ClimateVariable.Temperature, [], tempFormat);
        Mock<IVariableProcessor> vpdProcessor = CreateProcessor(ClimateVariable.Vpd, [tempFormat], vpdFormat);

        IEnumerable<IVariableProcessor> processors = [vpdProcessor.Object, tempProcessor.Object];

        List<IVariableProcessor> result = sorter.SortByDependencies(processors).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(tempProcessor.Object, result[0]);
        Assert.Equal(vpdProcessor.Object, result[1]);
    }

    [Fact]
    public void SortByDependencies_WithComplexDependencies_ReturnsCorrectOrder()
    {
        ClimateVariableFormat tempFormat = ClimateVariableFormat.Rechunked(ClimateVariable.Temperature);
        ClimateVariableFormat humidityFormat = ClimateVariableFormat.Rechunked(ClimateVariable.RelativeHumidity);
        ClimateVariableFormat vpdFormat = ClimateVariableFormat.Rechunked(ClimateVariable.Vpd);

        Mock<IVariableProcessor> tempProcessor = CreateProcessor(ClimateVariable.Temperature, [], tempFormat);
        Mock<IVariableProcessor> humidityProcessor = CreateProcessor(ClimateVariable.RelativeHumidity, [tempFormat], humidityFormat);
        Mock<IVariableProcessor> vpdProcessor = CreateProcessor(ClimateVariable.Vpd, [tempFormat, humidityFormat], vpdFormat);

        IEnumerable<IVariableProcessor> processors = [vpdProcessor.Object, humidityProcessor.Object, tempProcessor.Object];

        List<IVariableProcessor> result = sorter.SortByDependencies(processors).ToList();

        Assert.Equal(3, result.Count);
        // Temperature must come before humidity and vpd
        Assert.True(result.IndexOf(tempProcessor.Object) < result.IndexOf(humidityProcessor.Object));
        Assert.True(result.IndexOf(tempProcessor.Object) < result.IndexOf(vpdProcessor.Object));
        // Humidity must come before vpd
        Assert.True(result.IndexOf(humidityProcessor.Object) < result.IndexOf(vpdProcessor.Object));
    }

    [Fact]
    public void SortByDependencies_WithIntermediateOutputs_HandlesCorrectly()
    {
        ClimateVariableFormat tempTimeseriesFormat = ClimateVariableFormat.Timeseries(ClimateVariable.Temperature);
        ClimateVariableFormat tempRechunkedFormat = ClimateVariableFormat.Rechunked(ClimateVariable.Temperature);
        ClimateVariableFormat vpdFormat = ClimateVariableFormat.Rechunked(ClimateVariable.Vpd);

        Mock<IVariableProcessor> tempProcessor = CreateProcessorWithIntermediate(
            ClimateVariable.Temperature,
            [],
            tempRechunkedFormat,
            [tempTimeseriesFormat]);

        Mock<IVariableProcessor> vpdProcessor = CreateProcessor(
            ClimateVariable.Vpd,
            [tempTimeseriesFormat],
            vpdFormat);

        IEnumerable<IVariableProcessor> processors = [vpdProcessor.Object, tempProcessor.Object];

        List<IVariableProcessor> result = sorter.SortByDependencies(processors).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(tempProcessor.Object, result[0]);
        Assert.Equal(vpdProcessor.Object, result[1]);
    }

    [Fact]
    public void SortByDependencies_WithCircularDependency_ThrowsInvalidOperationException()
    {
        ClimateVariableFormat tempFormat = ClimateVariableFormat.Rechunked(ClimateVariable.Temperature);
        ClimateVariableFormat humidityFormat = ClimateVariableFormat.Rechunked(ClimateVariable.RelativeHumidity);

        Mock<IVariableProcessor> tempProcessor = CreateProcessor(ClimateVariable.Temperature, [humidityFormat], tempFormat);
        Mock<IVariableProcessor> humidityProcessor = CreateProcessor(ClimateVariable.RelativeHumidity, [tempFormat], humidityFormat);

        IEnumerable<IVariableProcessor> processors = [tempProcessor.Object, humidityProcessor.Object];

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            sorter.SortByDependencies(processors).ToList());

        Assert.Contains("Circular dependency", exception.Message);
    }

    [Fact]
    public void SortByDependencies_WithMissingDependency_ThrowsInvalidOperationException()
    {
        ClimateVariableFormat tempFormat = ClimateVariableFormat.Rechunked(ClimateVariable.Temperature);
        ClimateVariableFormat humidityFormat = ClimateVariableFormat.Rechunked(ClimateVariable.RelativeHumidity);

        Mock<IVariableProcessor> vpdProcessor = CreateProcessor(ClimateVariable.Vpd, [tempFormat, humidityFormat],
            ClimateVariableFormat.Rechunked(ClimateVariable.Vpd));

        IEnumerable<IVariableProcessor> processors = [vpdProcessor.Object];

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            sorter.SortByDependencies(processors).ToList());

        Assert.Contains("depends on", exception.Message);
    }

    [Fact]
    public void SortByDependencies_WithDuplicateOutputs_ThrowsInvalidOperationException()
    {
        ClimateVariableFormat tempFormat = ClimateVariableFormat.Rechunked(ClimateVariable.Temperature);

        Mock<IVariableProcessor> tempProcessor1 = CreateProcessor(ClimateVariable.Temperature, [], tempFormat);
        Mock<IVariableProcessor> tempProcessor2 = CreateProcessor(ClimateVariable.Temperature, [], tempFormat);

        IEnumerable<IVariableProcessor> processors = [tempProcessor1.Object, tempProcessor2.Object];

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            sorter.SortByDependencies(processors).ToList());

        Assert.Contains("Multiple processors produce", exception.Message);
    }

    /// <summary>
    /// Helper method to create a mock variable processor with the specified properties.
    /// </summary>
    /// <param name="variable">The target variable.</param>
    /// <param name="dependencies">The dependencies of the processor.</param>
    /// <returns>A mock variable processor.</returns>
    private static Mock<IVariableProcessor> CreateProcessor(
        ClimateVariable variable,
        IEnumerable<ClimateVariableFormat> dependencies)
    {
        return CreateProcessor(variable, dependencies, ClimateVariableFormat.Rechunked(variable));
    }

    /// <summary>
    /// Helper method to create a mock variable processor with the specified properties.
    /// </summary>
    /// <param name="variable">The target variable.</param>
    /// <param name="dependencies">The dependencies of the processor.</param>
    /// <param name="outputFormat">The output format of the processor.</param>
    /// <returns>A mock variable processor.</returns>
    private static Mock<IVariableProcessor> CreateProcessor(
        ClimateVariable variable,
        IEnumerable<ClimateVariableFormat> dependencies,
        ClimateVariableFormat outputFormat)
    {
        Mock<IVariableProcessor> processor = new Mock<IVariableProcessor>();
        processor.Setup(p => p.TargetVariable).Returns(variable);
        processor.Setup(p => p.OutputFormat).Returns(outputFormat);
        processor.Setup(p => p.Dependencies).Returns(new HashSet<ClimateVariableFormat>(dependencies));
        processor.Setup(p => p.IntermediateOutputs).Returns([]);
        return processor;
    }

    /// <summary>
    /// Helper method to create a mock variable processor with intermediate outputs.
    /// </summary>
    /// <param name="variable">The target variable.</param>
    /// <param name="dependencies">The dependencies of the processor.</param>
    /// <param name="outputFormat">The output format of the processor.</param>
    /// <param name="intermediateOutputs">The intermediate outputs of the processor.</param>
    /// <returns>A mock variable processor.</returns>
    private static Mock<IVariableProcessor> CreateProcessorWithIntermediate(
        ClimateVariable variable,
        IEnumerable<ClimateVariableFormat> dependencies,
        ClimateVariableFormat outputFormat,
        IEnumerable<ClimateVariableFormat> intermediateOutputs)
    {
        Mock<IVariableProcessor> processor = new Mock<IVariableProcessor>();
        processor.Setup(p => p.TargetVariable).Returns(variable);
        processor.Setup(p => p.OutputFormat).Returns(outputFormat);
        processor.Setup(p => p.Dependencies).Returns(new HashSet<ClimateVariableFormat>(dependencies));
        processor.Setup(p => p.IntermediateOutputs).Returns(intermediateOutputs);
        return processor;
    }
}

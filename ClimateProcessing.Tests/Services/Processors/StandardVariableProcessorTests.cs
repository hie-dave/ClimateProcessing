using ClimateProcessing.Models;
using ClimateProcessing.Models.Options;
using ClimateProcessing.Services;
using ClimateProcessing.Services.Processors;
using ClimateProcessing.Tests.Mocks;
using Moq;
using Xunit;

namespace ClimateProcessing.Tests.Services.Processors;

public sealed class StandardVariableProcessorTests
{
    [Fact]
    public async Task TestRenamedVariableAsync()
    {
        // When renaming a variable, the mergetime script will handle the
        // rename. The rechunk script therefore must use only the output
        // variable name.
        ClimateVariable variable = ClimateVariable.Precipitation;
        string inputVariableName = "inputVariableName";
        string outputVariableName = "outputVariableName";

        Mock<IMergetimeScriptGenerator> mergetimeProcessor = new();
        mergetimeProcessor
            .Setup(p => p.WriteMergetimeScriptAsync(
                It.IsAny<IFileWriter>(),
                It.IsAny<IMergetimeOptions>()))
            .Callback((IFileWriter writer, IMergetimeOptions options) =>
            {
                Assert.Equal(inputVariableName, options.InputMetadata.Name);
                Assert.Equal(outputVariableName, options.TargetMetadata.Name);
            });

        Mock<IRechunkScriptGenerator> rechunkProcessor = new();
        rechunkProcessor
            .Setup(p => p.WriteRechunkScriptAsync(
                It.IsAny<IFileWriter>(),
                It.IsAny<IRechunkOptions>()))
            .Callback((IFileWriter writer, IRechunkOptions options) =>
            {
                // The rechunk script must use only the output variable name.
                Assert.Equal(outputVariableName, options.VariableName);
            });

        StandardVariableProcessor processor = new StandardVariableProcessor(
            variable,
            mergetimeProcessor.Object,
            rechunkProcessor.Object
        );

        using TempDirectory tempDir = TempDirectory.Create();
        DynamicMockDataset mockDataset = new DynamicMockDataset(tempDir.AbsolutePath);
        mockDataset.SetVariableInfo(variable, inputVariableName, "mm");

        Mock<IClimateVariableManager> variableManager = new();
        variableManager.Setup(p => p.GetOutputRequirements(variable)).Returns(new VariableInfo(outputVariableName, "mm"));

        TestJobCreationContext context = new TestJobCreationContext();
        context.VariableManager = variableManager.Object;
        await processor.CreateJobsAsync(mockDataset, context);

        // Ensure that the rechunk callback was actually called.
        rechunkProcessor.Verify(p => p.WriteRechunkScriptAsync(
            It.IsAny<IFileWriter>(),
            It.IsAny<IRechunkOptions>()));
    }
}

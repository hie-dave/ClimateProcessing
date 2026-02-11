using ClimateProcessing.Models;
using ClimateProcessing.Models.Options;
using ClimateProcessing.Tests.Mocks;
using ClimateProcessing.Units;

namespace ClimateProcessing.Tests.Helpers;

/// <summary>
/// A mutable options implementation which can be used for testing. The default
/// parameters are set to perform as few operations as possible.
/// </summary>
public class MutableMergetimeOptions : IMergetimeOptions
{
    public string InputDirectory { get; set; } = "/input";
    public string OutputFile { get; set; } = "/output";
}

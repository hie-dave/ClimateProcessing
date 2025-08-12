using Xunit;

namespace ClimateProcessing.Tests.Helpers;

/// <summary>
/// Helper methods for assertions.
/// </summary>
public static class AssertionHelpers
{
    /// <summary>
    /// Assert that the specified directory is empty.
    /// </summary>
    /// <param name="directory">The directory to assert is empty.</param>
    public static void AssertEmptyDirectory(string directory)
    {
        Assert.True(Directory.Exists(directory));
        Assert.Empty(Directory.EnumerateFileSystemEntries(directory));
    }
}

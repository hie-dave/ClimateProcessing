using Xunit;

namespace ClimateProcessing.Tests.Helpers;

/// <summary>
/// Helper methods for assertions.
/// </summary>
public static class AssertionHelpers
{
    /// <summary>
    /// </summary>
    /// <param name="directory">The directory to assert is empty.</param>
    public static void AssertEmptyDirectory(string directory)
    {
        Assert.True(Directory.Exists(directory));
        Assert.Empty(Directory.EnumerateFileSystemEntries(directory));
    }

    public static void AssertScriptValid(string scriptContent)
    {
        // Every variable reference uses braces.
        Assert.DoesNotMatch(@"[^\\]\$[A-Za-z]", scriptContent);

        // No double-escaped braces from string interpolation
        Assert.DoesNotMatch(@"[^\\]\$\{\{", scriptContent);
        Assert.DoesNotMatch(@"[^\\]\$\{\{?[^\}]+\}\}", scriptContent);

        // We can't assume that all variable referenes are quoted, because
        // sometimes they don't need to be or shouldn't be.
    }
}

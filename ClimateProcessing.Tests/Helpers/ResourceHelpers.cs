using System.Reflection;

namespace ClimateProcessing.Tests.Helpers;

/// <summary>
/// Helper methods for reading resources.
/// </summary>
public static class ResourceHelpers
{
    /// <summary>
    /// Reads a resource embedded within the test assembly as a string.
    /// </summary>
    /// <param name="resourceName">The name of the resource to read.</param>
    /// <returns>The resource as a string.</returns>
    public static async Task<string> ReadResourceAsync(string resourceName)
    {
        Assembly assembly = typeof(ResourceHelpers).Assembly;
        return await ReadResourceAsync(resourceName, assembly);
    }

    /// <summary>
    /// Reads a resource embedded within the specified assembly as a string.
    /// </summary>
    /// <param name="resourceName">The name of the resource to read.</param>
    /// <param name="assembly">The assembly to read the resource from.</param>
    /// <returns>The resource as a string.</returns>
    /// <exception cref="ArgumentException">Thrown when the resource is not found.</exception>
    public static async Task<string> ReadResourceAsync(string resourceName, Assembly assembly)
    {
        string resource = $"ClimateProcessing.Tests.Data.{resourceName}";
        using Stream? stream = assembly.GetManifestResourceStream(resource);
        if (stream is null)
            throw new ArgumentException($"Resource {resource} not found.");

        using StreamReader reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }
}

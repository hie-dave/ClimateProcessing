using ClimateProcessing.Models;
using ClimateProcessing.Services;

namespace ClimateProcessing.Tests.Mocks;

public class MockPathManager : IPathManager
{
    private Action<IClimateDataset> createDirectoryTreeAction = _ => { };
    private readonly Dictionary<PathType, string> basePaths = new();
    private string checksumFilePath = string.Empty;

    public MockPathManager()
    {
        foreach (PathType type in Enum.GetValues<PathType>())
            basePaths[type] = Enum.GetName(type)!;
    }

    public void SetCreateDirectoryTreeAction(Action<IClimateDataset> action)
    {
        createDirectoryTreeAction = action;
    }

    public void SetBasePath(PathType pathType, string path)
    {
        basePaths[pathType] = path;
    }

    public void SetChecksumFilePath(string path)
    {
        checksumFilePath = path;
    }

    public void CreateDirectoryTree(IClimateDataset dataset)
    {
        createDirectoryTreeAction(dataset);
    }

    public string GetBasePath(PathType pathType)
    {
        if (!basePaths.TryGetValue(pathType, out string? basePath))
            return string.Empty;
        return basePath;
    }

    public string GetChecksumFilePath()
    {
        return checksumFilePath;
    }

    public string GetDatasetFileName(IClimateDataset dataset, ClimateVariable variable, PathType type)
    {
        return Path.Combine(GetDatasetPath(dataset, type), $"{Enum.GetName(variable)!}.nc");
    }

    public string GetDatasetPath(IClimateDataset dataset, PathType type)
    {
        return Path.Combine(GetBasePath(type), dataset.DatasetName);
    }
}

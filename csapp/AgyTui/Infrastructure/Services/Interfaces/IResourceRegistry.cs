namespace AgyTui.Infrastructure.Services;

public interface IResourceRegistry
{
    ResourceEntry[] LoadAll();
    void Save(ResourceEntry[] entries);
    string AddResource(string path, string[] tags);
    void UpdateStatus(string id, string status, string? error = null);
    string ComputeChecksum(string filePath);
    string DetectFormat(string path);
}

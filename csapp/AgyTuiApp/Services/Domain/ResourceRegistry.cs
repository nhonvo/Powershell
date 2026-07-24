using System;
using System.IO;
using System.Linq;
using AgyTui.Components;

namespace AgyTui;

public sealed record ResourceEntry(string Id, string Path, string Format, string Title, string[] Tags, string[] Topics, string Language, string SourceType, string? Checksum, long SizeBytes, string AddedAt, string? LastExtractedAt, string Status, string? ErrorMessage, int ExtractedItemCount, string[] LearnFiles, bool AutoDiscovered, bool Enabled);

public sealed record ExtractionConfig(string LearnPath, string VaultPath, string ResourcesIndexPath, bool DryRun, bool ForceReExtract);

public sealed record ResourceIndex(int Version, string UpdatedAt, ResourceEntry[] Resources);

public static class ResourceRegistry
{
    public static ResourceEntry[] LoadAll()
    {
        var idx = LearnDataPaths.LoadJson<ResourceIndex>(LearnDataPaths.ResourcesIndex);
        return idx?.Resources ?? [];
    }

    public static void Save(ResourceEntry[] entries)
    {
        var idx = new ResourceIndex(1, DateTimeOffset.Now.ToString("o"), entries);
        LearnDataPaths.SaveJson(LearnDataPaths.ResourcesIndex, idx);
    }

    public static string AddResource(string path, string[] tags)
    {
        var entries = LoadAll().ToList();
        var id = $"res_{entries.Count + 1:000}";
        var format = DetectFormat(path);
        var checksum = File.Exists(path) ? ComputeChecksum(path) : null;
        var size = File.Exists(path) ? new FileInfo(path).Length : 0;
        entries.Add(new ResourceEntry(id, path, format, System.IO.Path.GetFileNameWithoutExtension(path), tags, [], "auto", "local_file", checksum, size, DateTimeOffset.Now.ToString("o"), null, "pending", null, 0, [], false, true));
        Save([.. entries]);
        return id;
    }

    public static void UpdateStatus(string id, string status, string? error = null)
    {
        var entries = LoadAll().ToList();
        var idx = entries.FindIndex(e => e.Id == id);
        if (idx < 0) return;
        entries[idx] = entries[idx] with
        {
            Status = status,
            ErrorMessage = error
        };
        Save([.. entries]);
    }

    public static string ComputeChecksum(string filePath)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        using var stream = File.OpenRead(filePath);
        return "sha256:" + Convert.ToHexString(sha.ComputeHash(stream));
    }

    public static string DetectFormat(string path)
    {
        if (path.StartsWith("http")) return "url";
        return System.IO.Path.GetExtension(path).ToLower() switch
        {
            ".md" or ".txt" => "md",
            ".pdf" => "pdf",
            ".docx" => "docx",
            ".csv" => "csv",
            ".tsv" => "tsv",
            ".epub" => "epub",
            ".cs" or ".py" or ".ts" or ".js" or ".go" => "code",
            ".png" or ".jpg" or ".jpeg" => "image",
            _ => "md"
        };
    }
}

public static class ResourceScanner
{
    public static string[] FindNotesByTag(string vaultPath, string[] tags)
    {
        if (!Directory.Exists(vaultPath)) return [];
        return Directory.GetFiles(vaultPath, "*.md", SearchOption.AllDirectories).Where(f =>
        {
            var fm = ObsidianBridge.ParseFrontmatter(f);
            return fm != null && fm.Tags.Any(t => tags.Any(needle => t.Contains(needle, StringComparison.OrdinalIgnoreCase)));
        }).ToArray();
    }

    public static string[] FindNotesByTopic(string vaultPath, string topic)
    {
        if (!Directory.Exists(vaultPath)) return [];
        return Directory.GetFiles(vaultPath, "*.md", SearchOption.AllDirectories).Where(f =>
        {
            var fm = ObsidianBridge.ParseFrontmatter(f);
            return fm != null && fm.Topic.Equals(topic, StringComparison.OrdinalIgnoreCase);
        }).ToArray();
    }

    public static string[] ListAllTags(string vaultPath)
    {
        if (!Directory.Exists(vaultPath)) return [];
        return Directory.GetFiles(vaultPath, "*.md", SearchOption.AllDirectories).SelectMany(f => ObsidianBridge.ParseFrontmatter(f)?.Tags ?? []).Distinct().OrderBy(t => t).ToArray();
    }
}

using AgyTui.Core.Models;
using AgyTui.Infrastructure.Integrations.Ai.Abstractions;

namespace AgyTui.Infrastructure.Integrations.Ai.Services;

public sealed record ProjectScanResult(string Name, string Path, string Provider, string[] Features);

public class AiProjectScanner : IAiProjectScanner
{
    public ProjectScanResult[] ScanProjectsForClaude(string? baseDir = null)
    {
        return ScanProjectsByFilter("claude", dir =>
            File.Exists(Path.Combine(dir, "CLAUDE.md")) ||
            Directory.Exists(Path.Combine(dir, ".claude")) ||
            File.Exists(Path.Combine(dir, "package.json")), baseDir);
    }

    public ProjectScanResult[] ScanProjectsForOllama(string? baseDir = null)
    {
        return ScanProjectsByFilter("ollama", dir =>
            File.Exists(Path.Combine(dir, "Modelfile")) ||
            Directory.Exists(Path.Combine(dir, ".ollama")), baseDir);
    }

    public ProjectScanResult[] ScanProjectsForAgy(string? baseDir = null)
    {
        return ScanProjectsByFilter("agy", dir =>
            File.Exists(Path.Combine(dir, "AGY.md")) ||
            File.Exists(Path.Combine(dir, "agy.json")) ||
            Directory.Exists(Path.Combine(dir, ".gemini")), baseDir);
    }

    public ProjectScanResult[] ScanProjects(string provider, string? baseDir = null)
    {
        return (provider?.ToLowerInvariant()) switch
        {
            "claude" => ScanProjectsForClaude(baseDir),
            "ollama" => ScanProjectsForOllama(baseDir),
            "agy" or "gemini" => ScanProjectsForAgy(baseDir),
            _ => ScanProjectsByFilter("all", _ => true, baseDir)
        };
    }

    private static ProjectScanResult[] ScanProjectsByFilter(string provider, Func<string, bool> matchPredicate, string? baseDir = null)
    {
        var results = new List<ProjectScanResult>();
        var candidateRoots = new List<string>();

        if (!string.IsNullOrEmpty(baseDir) && Directory.Exists(baseDir)) candidateRoots.Add(baseDir);
        if (!string.IsNullOrEmpty(Config.Current.ProjectsBaseDir) && Directory.Exists(Config.Current.ProjectsBaseDir)) candidateRoots.Add(Config.Current.ProjectsBaseDir);
        if (Directory.Exists(@"C:\Users\sshuser\project")) candidateRoots.Add(@"C:\Users\sshuser\project");

        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (Directory.Exists(Path.Combine(userProfile, "project"))) candidateRoots.Add(Path.Combine(userProfile, "project"));
        if (Directory.Exists(Path.Combine(userProfile, "Desktop", "project"))) candidateRoots.Add(Path.Combine(userProfile, "Desktop", "project"));
        if (candidateRoots.Count == 0) candidateRoots.Add(Directory.GetCurrentDirectory());

        var scannedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var searchRoot in candidateRoots)
        {
            try
            {
                foreach (var dir in Directory.GetDirectories(searchRoot))
                {
                    try
                    {
                        var dirName = Path.GetFileName(dir);
                        if (dirName.StartsWith(".") || dirName.Equals("node_modules", StringComparison.OrdinalIgnoreCase)) continue;
                        if (scannedPaths.Add(dir) && matchPredicate(dir))
                        {
                            results.Add(new ProjectScanResult(dirName, dir, provider, new[] { provider, "scanned" }));
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        return results.ToArray();
    }
}

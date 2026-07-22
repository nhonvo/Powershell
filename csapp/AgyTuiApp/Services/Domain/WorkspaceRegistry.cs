using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using Spectre.Console;
using AgyTui.Components;

namespace AgyTui;

public sealed record WorkspaceEntry(string Name, [property: JsonPropertyName("Path")] string WorkspacePath, string? AssociatedAccount, string[]? Tags);

public static class WorkspaceRegistry
{
    private static readonly TtlCache<string, WorkspaceEntry[]> _cache = new(TimeSpan.FromSeconds(5));

    private static string ConfigFile => System.IO.Path.Combine(
        AgyAccountCore.AgySourceHome, "antigravity", "priority_workspaces.json");

    public static WorkspaceEntry[] GetWorkspaces()
    {
        return _cache.GetOrCompute("workspaces", () =>
        {
            WorkspaceEntry[] items = [];
            if (File.Exists(ConfigFile))
            {
                try
                {
                    var raw = File.ReadAllText(ConfigFile);
                    items = JsonSerializer.Deserialize<WorkspaceEntry[]>(raw)?.Where(w => w != null && !string.IsNullOrEmpty(w.WorkspacePath)).ToArray() ?? [];
                }
                catch {}
            }

            if (items.Length == 0)
            {
                items = AutoDiscoverWorkspaces();
                if (items.Length > 0) SaveWorkspaces(items);
            }

            return items;
        });
    }

    public static WorkspaceEntry[] AutoDiscoverWorkspaces()
    {
        var list = new List<WorkspaceEntry>();
        var addedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void TryAdd(string name, string path)
        {
            if (!string.IsNullOrEmpty(path) && Directory.Exists(path) && addedPaths.Add(path))
            {
                list.Add(new WorkspaceEntry(name, path, "default", new[] { "auto-discovered" }));
            }
        }

        // 1. Current working directory
        try
        {
            var currentDir = Directory.GetCurrentDirectory();
            TryAdd(System.IO.Path.GetFileName(currentDir), currentDir);
        }
        catch {}

        // 2. PowerShell profile root
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        TryAdd("Powershell Profile", System.IO.Path.Combine(userProfile, "Documents", "Powershell"));

        // 3. Candidate base project directories
        var searchBases = new List<string>();
        if (!string.IsNullOrEmpty(Config.Current.ProjectsBaseDir)) searchBases.Add(Config.Current.ProjectsBaseDir);
        searchBases.Add(System.IO.Path.Combine(userProfile, "project"));
        searchBases.Add(System.IO.Path.Combine(userProfile, "Documents"));
        searchBases.Add(System.IO.Path.Combine(userProfile, "Desktop"));

        foreach (var baseDir in searchBases)
        {
            if (!Directory.Exists(baseDir)) continue;
            try
            {
                var subDirs = Directory.GetDirectories(baseDir);
                foreach (var dir in subDirs)
                {
                    try
                    {
                        var dirName = System.IO.Path.GetFileName(dir);
                        if (dirName.StartsWith(".") || dirName.Equals("node_modules", StringComparison.OrdinalIgnoreCase)) continue;

                        if (Directory.Exists(System.IO.Path.Combine(dir, ".git")) ||
                            Directory.GetFiles(dir, "*.csproj").Length > 0 ||
                            Directory.GetFiles(dir, "*.sln").Length > 0 ||
                            File.Exists(System.IO.Path.Combine(dir, "package.json")))
                        {
                            TryAdd(dirName, dir);
                        }
                    }
                    catch {}
                }
            }
            catch {}
        }

        return list.ToArray();
    }

    public static void SaveWorkspaces(WorkspaceEntry[] entries)
    {
        try
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(ConfigFile)!);
            File.WriteAllText(ConfigFile, JsonSerializer.Serialize(entries, new JsonSerializerOptions
            {
                WriteIndented = true
            }
            ), Encoding.UTF8);
            _cache.Clear();
        }
        catch (Exception ex)
        {
            SpectrePanel.Error($"Failed to save workspaces: {ex.Message}");
        }

    }

    public static WorkspaceEntry[] FindByQuery(string query, bool asRegex = false)
    {
        var all = GetWorkspaces();
        if (string.IsNullOrWhiteSpace(query)) return all;

        if (asRegex)
        {
            try
            {
                return all.Where(w => Regex.IsMatch(w.Name, query, RegexOptions.IgnoreCase) || Regex.IsMatch(w.WorkspacePath, query, RegexOptions.IgnoreCase)).ToArray();
            }
            catch
            {
                return [];
            }
        }

        return all.Where(w => w.Name.Contains(query, StringComparison.OrdinalIgnoreCase) || w.WorkspacePath.Contains(query, StringComparison.OrdinalIgnoreCase)).ToArray();
    }

    public static WorkspaceEntry[] GetByAccount(string accountName)
    {
        var targetAccount = string.IsNullOrEmpty(accountName) ? "default" : accountName;
        return GetWorkspaces().Where(w => string.Equals(w.AssociatedAccount ?? "default", targetAccount, StringComparison.OrdinalIgnoreCase)).ToArray();
    }

    public static string GetGitBranch(string dirPath)
    {
        try
        {
            var gitPath = System.IO.Path.Combine(dirPath, ".git");
            string headFile = System.IO.Path.Combine(gitPath, "HEAD");

            if (File.Exists(gitPath) && !Directory.Exists(gitPath))
            {
                var lines = File.ReadAllLines(gitPath);
                var gitdirLine = lines.FirstOrDefault(l => l.StartsWith("gitdir:", StringComparison.OrdinalIgnoreCase));
                if (gitdirLine != null)
                {
                    var targetGitDir = gitdirLine.Substring("gitdir:".Length).Trim();
                    if (!System.IO.Path.IsPathRooted(targetGitDir))
                    {
                        targetGitDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(dirPath, targetGitDir));
                    }
                    headFile = System.IO.Path.Combine(targetGitDir, "HEAD");
                }
            }

            if (File.Exists(headFile))
            {
                var txt = File.ReadAllText(headFile).Trim();
                if (txt.StartsWith("ref: refs/heads/"))
                {
                    return txt.Substring("ref: refs/heads/".Length);
                }
                if (txt.Length >= 7) return txt.Substring(0, 7);
            }
        }
        catch { }
        return "";
    }

    public static readonly string[] SharedWorkspaceActions = new[]
    {
        "📂 Change Directory on exit",
        "🚀 Launch New Terminal Session (wt / pwsh)",
        "💻 Open in Terminal IDE (/ide)",
        "📁 Open in Windows File Explorer",
        "🔀 View Git Status & Diff"
    };

    public static string HandleWorkspaceAction(WorkspaceEntry selected, int actionIdx)
    {
        if (actionIdx == 0)
        {
            var agyHome = AgyAccountCore.AgySourceHome;
            Directory.CreateDirectory(agyHome);
            var selectedProjFile = System.IO.Path.Combine(agyHome, "selected_project.txt");
            File.WriteAllText(selectedProjFile, selected.WorkspacePath);
            SpectrePanel.Success($"Selected workspace '{selected.Name}'. Directory switch will apply on exit.");
            Thread.Sleep(1000);
            return selected.WorkspacePath;
        }
        else if (actionIdx == 1)
        {
            SystemHelper.OpenNewTerminalSession(selected.WorkspacePath);
            return selected.WorkspacePath;
        }
        else if (actionIdx == 2)
        {
            TerminalIde.Open(selected.WorkspacePath);
            return selected.WorkspacePath;
        }
        else if (actionIdx == 3)
        {
            SystemHelper.OpenExplorer(selected.WorkspacePath);
            return selected.WorkspacePath;
        }
        else if (actionIdx == 4)
        {
            GitDiffViewer.ShowDiff(selected.WorkspacePath);
            return selected.WorkspacePath;
        }
        return selected.WorkspacePath;
    }
}

public static class ProfileNavigator
{
    public static string? Navigate(string query) => Navigate(query, WorkspaceRegistry.GetWorkspaces());

    public static string? Navigate(string query, WorkspaceEntry[] workspaces)
    {
        if (workspaces.Length == 0)
        {
            SpectrePanel.Warning("No workspaces registered.");
            return null;
        }
        WorkspaceEntry[] matches;
        if (string.IsNullOrWhiteSpace(query)) matches = workspaces;
        else
        {
            matches = WorkspaceRegistry.FindByQuery(query);
            if (matches.Length == 0)
            {
                SpectrePanel.Warning($"No workspace matched '{query}'.");
                return null;
            }
        }

        WorkspaceEntry selected;
        if (matches.Length == 1)
        {
            selected = matches[0];
        }
        else
        {
            var menuItems = matches.Select(m =>
            {
                var branch = WorkspaceRegistry.GetGitBranch(m.WorkspacePath);
                var branchSuffix = !string.IsNullOrEmpty(branch) ? $" [{branch}]" : "";
                return $"{m.Name}{branchSuffix} — {m.WorkspacePath}";
            }).ToArray();

            var idx = SpectreMenu.Show("Select Workspace Target", menuItems, 0, true);
            if (idx < 0) return null;
            selected = matches[idx];
        }

        var actionIdx = SpectreMenu.ShowWithEscape($"Workspace: {selected.Name}", WorkspaceRegistry.SharedWorkspaceActions, 0);
        if (actionIdx < 0) return selected.WorkspacePath;
        return WorkspaceRegistry.HandleWorkspaceAction(selected, actionIdx);
    }
}

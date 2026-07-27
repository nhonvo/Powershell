using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace AgyTui.Core.Registries;

public sealed record WorkspaceLink(string Label, string Url);

public sealed record WorkspaceEntry(
    string Name,
    [property: JsonPropertyName("Path")] string WorkspacePath,
    string? AssociatedAccount,
    string[]? Tags,
    WorkspaceLink[]? Links = null,
    string? Alias = null
);

public static class WorkspaceRegistry
{
    private static readonly TtlCache<string, WorkspaceEntry[]> _cache = new(TimeSpan.FromSeconds(5));

    private static string ConfigFile
    {
        get
        {
            var repoRoot = Config.GetProfileRepoRoot();
            var rootFile = Path.Combine(repoRoot, "priority_workspaces.json");
            if (File.Exists(rootFile)) return rootFile;

            var csappFile = Path.Combine(repoRoot, "csapp", "priority_workspaces.json");
            if (File.Exists(csappFile)) return csappFile;

            var legacyFile = Path.Combine(AgyAccountCore.AgySourceHome, "antigravity", "priority_workspaces.json");
            if (File.Exists(legacyFile)) return legacyFile;

            return rootFile;
        }
    }

    public static string DeriveAlias(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "proj";
        var clean = Regex.Replace(name, @"[^a-zA-Z0-9]", "").ToLowerInvariant();
        return string.IsNullOrEmpty(clean) ? "proj" : clean;
    }

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
                    items = JsonSerializer.Deserialize<WorkspaceEntry[]>(raw)?
                        .Where(w => w != null && !string.IsNullOrEmpty(w.WorkspacePath) && Directory.Exists(w.WorkspacePath))
                        .Select(w => string.IsNullOrEmpty(w.Alias) ? w with { Alias = DeriveAlias(w.Name) } : w)
                        .ToArray() ?? [];
                }
                catch { }
            }

            if (items.Length == 0)
            {
                items = AutoDiscoverWorkspaces();
                if (items.Length > 0) SaveWorkspaces(items);
            }

            return items;
        });
    }

    public static int PruneWorkspaces()
    {
        if (!File.Exists(ConfigFile)) return 0;
        try
        {
            var raw = File.ReadAllText(ConfigFile);
            var items = JsonSerializer.Deserialize<WorkspaceEntry[]>(raw) ?? [];
            var valid = items.Where(w => w != null && !string.IsNullOrEmpty(w.WorkspacePath) && Directory.Exists(w.WorkspacePath))
                             .Select(w => string.IsNullOrEmpty(w.Alias) ? w with { Alias = DeriveAlias(w.Name) } : w)
                             .ToArray();
            var prunedCount = items.Length - valid.Length;
            if (prunedCount > 0)
            {
                SaveWorkspaces(valid);
            }
            return prunedCount;
        }
        catch { return 0; }
    }

    public static WorkspaceEntry[] AutoDiscoverWorkspaces()
    {
        var list = new List<WorkspaceEntry>();
        var addedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void TryAdd(string name, string path)
        {
            if (!string.IsNullOrEmpty(path) && Directory.Exists(path) && addedPaths.Add(path))
            {
                var alias = DeriveAlias(name);
                list.Add(new WorkspaceEntry(name, path, "default", new[] { "auto-discovered" }, null, alias));
            }
        }

        // 1. Current working directory
        try
        {
            var currentDir = Directory.GetCurrentDirectory();
            TryAdd(Path.GetFileName(currentDir), currentDir);
        }
        catch { }

        // 2. PowerShell profile root
        var userProfile = AppPaths.UserProfileDir;
        TryAdd("Powershell Profile", Path.Combine(userProfile, "Documents", "Powershell"));

        // 3. Candidate base project directories (including C:\Users\sshuser\project)
        var searchBases = new List<string>();
        if (!string.IsNullOrEmpty(Config.Current.ProjectsBaseDir)) searchBases.Add(Config.Current.ProjectsBaseDir);
        searchBases.Add(@"C:\Users\sshuser\project");
        searchBases.Add(Path.Combine(userProfile, "project"));
        searchBases.Add(Path.Combine(userProfile, "Documents"));
        searchBases.Add(Path.Combine(userProfile, "Desktop"));
        searchBases.Add(Path.Combine(userProfile, "Desktop", "project"));

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
                        var dirName = Path.GetFileName(dir);
                        if (dirName.StartsWith(".") || dirName.Equals("node_modules", StringComparison.OrdinalIgnoreCase)) continue;

                        if (Directory.Exists(Path.Combine(dir, ".git")) ||
                            Directory.GetFiles(dir, "*.csproj").Length > 0 ||
                            Directory.GetFiles(dir, "*.sln").Length > 0 ||
                            File.Exists(Path.Combine(dir, "package.json")))
                        {
                            TryAdd(dirName, dir);
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        return list.ToArray();
    }

    public static int SyncAllProjects(string? customBaseDir = null)
    {
        var list = new List<WorkspaceEntry>();
        var addedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void TryAdd(string name, string path)
        {
            if (!string.IsNullOrEmpty(path) && Directory.Exists(path) && addedPaths.Add(path))
            {
                var alias = DeriveAlias(name);
                list.Add(new WorkspaceEntry(name, path, "default", new[] { "scanned" }, null, alias));
            }
        }

        var repoRoot = Config.GetProfileRepoRoot();
        if (Directory.Exists(repoRoot)) TryAdd("Powershell", repoRoot);

        var searchBases = new List<string>();
        if (!string.IsNullOrEmpty(customBaseDir) && Directory.Exists(customBaseDir)) searchBases.Add(customBaseDir);
        if (!string.IsNullOrEmpty(Config.Current.ProjectsBaseDir) && Directory.Exists(Config.Current.ProjectsBaseDir)) searchBases.Add(Config.Current.ProjectsBaseDir);
        searchBases.Add(@"C:\Users\sshuser\project");

        var userProfile = AppPaths.UserProfileDir;
        if (Directory.Exists(Path.Combine(userProfile, "project"))) searchBases.Add(Path.Combine(userProfile, "project"));
        if (Directory.Exists(Path.Combine(userProfile, "Desktop", "project"))) searchBases.Add(Path.Combine(userProfile, "Desktop", "project"));

        foreach (var baseDir in searchBases)
        {
            if (!Directory.Exists(baseDir)) continue;
            try
            {
                foreach (var dir in Directory.GetDirectories(baseDir))
                {
                    try
                    {
                        var dirName = Path.GetFileName(dir);
                        if (dirName.StartsWith(".") || dirName.Equals("node_modules", StringComparison.OrdinalIgnoreCase)) continue;
                        TryAdd(dirName, dir);
                    }
                    catch { }
                }
            }
            catch { }
        }

        if (list.Count > 0)
        {
            SaveWorkspaces(list.ToArray());
        }
        return list.Count;
    }

    public static void SaveWorkspaces(WorkspaceEntry[] entries)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigFile)!);
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
                return all.Where(w =>
                    Regex.IsMatch(w.Name, query, RegexOptions.IgnoreCase) ||
                    (!string.IsNullOrEmpty(w.Alias) && Regex.IsMatch(w.Alias, query, RegexOptions.IgnoreCase)) ||
                    Regex.IsMatch(w.WorkspacePath, query, RegexOptions.IgnoreCase)
                ).ToArray();
            }
            catch
            {
                return [];
            }
        }

        return all.Where(w =>
            w.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            (!string.IsNullOrEmpty(w.Alias) && w.Alias.Equals(query, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrEmpty(w.Alias) && w.Alias.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
            w.WorkspacePath.Contains(query, StringComparison.OrdinalIgnoreCase)
        ).ToArray();
    }

    public static WorkspaceEntry[] GetByAccount(string accountName)
    {
        var targetAccount = string.IsNullOrEmpty(accountName) ? "default" : accountName;
        return GetWorkspaces().Where(w => string.Equals(w.AssociatedAccount ?? "default", targetAccount, StringComparison.OrdinalIgnoreCase)).ToArray();
    }

    private static readonly TtlCache<string, string> _branchCache = new(TimeSpan.FromSeconds(5));

    public static string GetGitBranch(string dirPath)
    {
        if (string.IsNullOrEmpty(dirPath)) return "";
        return _branchCache.GetOrCompute(dirPath, () =>
        {
            string branch = "";
            try
            {
                var gitPath = Path.Combine(dirPath, ".git");
                string headFile = Path.Combine(gitPath, "HEAD");

                if (File.Exists(gitPath) && !Directory.Exists(gitPath))
                {
                    var lines = File.ReadAllLines(gitPath);
                    var gitdirLine = lines.FirstOrDefault(l => l.StartsWith("gitdir:", StringComparison.OrdinalIgnoreCase));
                    if (gitdirLine != null)
                    {
                        var targetGitDir = gitdirLine.Substring("gitdir:".Length).Trim();
                        if (!Path.IsPathRooted(targetGitDir))
                        {
                            targetGitDir = Path.GetFullPath(Path.Combine(dirPath, targetGitDir));
                        }
                        headFile = Path.Combine(targetGitDir, "HEAD");
                    }
                }

                if (File.Exists(headFile))
                {
                    var txt = File.ReadAllText(headFile).Trim();
                    if (txt.StartsWith("ref: refs/heads/"))
                    {
                        branch = txt.Substring("ref: refs/heads/".Length);
                    }
                    else if (txt.Length >= 7)
                    {
                        branch = txt.Substring(0, 7);
                    }
                }
            }
            catch { }

            return branch;
        });
    }

    public static readonly string[] SharedWorkspaceActions = new[]
    {
        "📂 Change Directory to workspace",
        "🚀 Open in New Terminal",
        "💻 Open in Terminal IDE (/ide)",
        "📁 Open in Windows File Explorer",
        "🔀 View Git Status & Diff",
        "🤖 Start Antigravity AI Agent (ask-ai)",
        "🛸 Open Antigravity TUI / Deck",
        "📦 Clean & Rebuild Project (.NET)",
        "🕸 Open Git Nexus Dashboard",
        "📊 View Git Nexus Commit Stats",
        "🔗 Manage/Open Project Links"
    };

    public static string HandleWorkspaceAction(WorkspaceEntry selected, int actionIdx)
    {
        if (actionIdx == 0)
        {
            var agyHome = AgyAccountCore.AgySourceHome;
            Directory.CreateDirectory(agyHome);
            var selectedProjFile = Path.Combine(agyHome, "selected_project.txt");
            File.WriteAllText(selectedProjFile, selected.WorkspacePath);
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
        else if (actionIdx == 5)
        {
            SystemHelper.OpenNewTerminalSession(selected.WorkspacePath, "ask-ai");
            return selected.WorkspacePath;
        }
        else if (actionIdx == 6)
        {
            SystemHelper.OpenNewTerminalSession(selected.WorkspacePath, "cc");
            return selected.WorkspacePath;
        }
        else if (actionIdx == 7)
        {
            var projFiles = Directory.GetFiles(selected.WorkspacePath, "*.csproj", SearchOption.AllDirectories);
            if (projFiles.Length > 0)
            {
                AnsiConsole.Clear();
                AnsiConsole.MarkupLine("[bold cyan]🔨 Building .NET Projects...[/]\n");
                Helpers.ProcessRunner.Run("dotnet", "build", selected.WorkspacePath);
                AnsiConsole.MarkupLine("\n[dim]Press any key to return...[/]");
                Console.ReadKey(true);
            }
            else
            {
                SpectrePanel.Warning("No C# project (.csproj) found in this workspace.");
                Thread.Sleep(1500);
            }
            return selected.WorkspacePath;
        }
        else if (actionIdx == 8)
        {
            GitNexus.ShowLiveDashboard();
            return selected.WorkspacePath;
        }
        else if (actionIdx == 9)
        {
            GitNexusStats.Run();
            return selected.WorkspacePath;
        }
        else if (actionIdx == 10)
        {
            ManageWorkspaceLinks(selected);
            return selected.WorkspacePath;
        }
        return selected.WorkspacePath;
    }

    public static void OpenUrl(string url)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
            SpectrePanel.Success($"Opening URL in browser: {url}");
            Thread.Sleep(1000);
        }
        catch (Exception ex)
        {
            SpectrePanel.Error($"Failed to open URL: {ex.Message}");
            Thread.Sleep(1500);
        }
    }

    private static void UpdateWorkspaceLinks(string workspacePath, WorkspaceLink[] newLinks)
    {
        var all = GetWorkspaces().ToList();
        var idx = all.FindIndex(w => string.Equals(w.WorkspacePath, workspacePath, StringComparison.OrdinalIgnoreCase));
        if (idx >= 0)
        {
            var old = all[idx];
            all[idx] = old with { Links = newLinks };
            SaveWorkspaces(all.ToArray());
        }
    }

    public static void ManageWorkspaceLinks(WorkspaceEntry selected)
    {
        while (true)
        {
            var workspaces = GetWorkspaces();
            var currentWs = workspaces.FirstOrDefault(w => string.Equals(w.WorkspacePath, selected.WorkspacePath, StringComparison.OrdinalIgnoreCase)) ?? selected;
            var linksList = currentWs.Links != null ? currentWs.Links.ToList() : new List<WorkspaceLink>();

            var menuOptions = new List<string>();
            foreach (var link in linksList)
            {
                menuOptions.Add($"🌐 Open: {link.Label} ({link.Url})");
            }
            menuOptions.Add("➕ Add New Link");
            if (linksList.Count > 0)
            {
                menuOptions.Add("✏️ Edit Link");
                menuOptions.Add("❌ Delete Link");
            }
            menuOptions.Add("↩ Back");

            var choice = SpectreMenu.Show($"Links for {currentWs.Name}", menuOptions.ToArray(), 0);
            if (choice < 0 || choice == menuOptions.Count - 1)
            {
                break; // Back
            }

            if (choice < linksList.Count)
            {
                var targetLink = linksList[choice];
                OpenUrl(targetLink.Url);
            }
            else
            {
                var action = menuOptions[choice];
                if (action == "➕ Add New Link")
                {
                    Console.CursorVisible = true;
                    var label = AnsiConsole.Ask<string>("Enter Link Label (e.g. prod api, prod ui, db):").Trim();
                    var url = AnsiConsole.Ask<string>("Enter Link URL:").Trim();
                    Console.CursorVisible = false;

                    if (!string.IsNullOrEmpty(label) && !string.IsNullOrEmpty(url))
                    {
                        linksList.Add(new WorkspaceLink(label, url));
                        UpdateWorkspaceLinks(currentWs.WorkspacePath, linksList.ToArray());
                        SpectrePanel.Success("Link added successfully!");
                        Thread.Sleep(1000);
                    }
                }
                else if (action == "✏️ Edit Link")
                {
                    var editOptions = linksList.Select(l => $"{l.Label} ({l.Url})").ToArray();
                    var editChoice = SpectreMenu.Show("Select Link to Edit", editOptions, 0);
                    if (editChoice >= 0 && editChoice < linksList.Count)
                    {
                        var target = linksList[editChoice];
                        Console.CursorVisible = true;
                        var label = AnsiConsole.Prompt(new TextPrompt<string>("Enter Link Label:").DefaultValue(target.Label)).Trim();
                        var url = AnsiConsole.Prompt(new TextPrompt<string>("Enter Link URL:").DefaultValue(target.Url)).Trim();
                        Console.CursorVisible = false;

                        if (!string.IsNullOrEmpty(label) && !string.IsNullOrEmpty(url))
                        {
                            linksList[editChoice] = new WorkspaceLink(label, url);
                            UpdateWorkspaceLinks(currentWs.WorkspacePath, linksList.ToArray());
                            SpectrePanel.Success("Link updated successfully!");
                            Thread.Sleep(1000);
                        }
                    }
                }
                else if (action == "❌ Delete Link")
                {
                    var deleteOptions = linksList.Select(l => $"{l.Label} ({l.Url})").ToArray();
                    var deleteChoice = SpectreMenu.Show("Select Link to Delete", deleteOptions, 0);
                    if (deleteChoice >= 0 && deleteChoice < linksList.Count)
                    {
                        linksList.RemoveAt(deleteChoice);
                        UpdateWorkspaceLinks(currentWs.WorkspacePath, linksList.ToArray());
                        SpectrePanel.Success("Link deleted successfully!");
                        Thread.Sleep(1000);
                    }
                }
            }
        }
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
                var aliasTag = !string.IsNullOrEmpty(m.Alias) ? $" ({m.Alias})" : "";
                return $"{m.Name}{aliasTag}{branchSuffix} — {m.WorkspacePath}";
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

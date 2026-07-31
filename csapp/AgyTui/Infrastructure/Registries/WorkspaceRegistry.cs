using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using AgyTui.Domain.WorkspaceContext;

namespace AgyTui.Infrastructure.Registries;

public static class WorkspaceRegistry
{
    private static readonly TtlCache<string, WorkspaceEntry[]> WorkspacesCache = new(TimeSpan.FromSeconds(5));

    private static string ConfigFile
    {
        get
        {
            var repoRoot = Config.GetProfileRepoRoot();
            var rootFile = Path.Combine(repoRoot, "priority_workspaces.json");
            if (File.Exists(rootFile)) return rootFile;

            var csappFile = Path.Combine(repoRoot, "csapp", "priority_workspaces.json");
            if (File.Exists(csappFile)) return csappFile;

            var legacyFile = Path.Combine(AppPaths.GeminiHome, "antigravity", "priority_workspaces.json");
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

    public static WorkspaceAggregate[] GetWorkspaceAggregates()
    {
        return GetWorkspaces().Select(w => WorkspaceAggregate.FromEntry(w, false, GetGitBranch(w.WorkspacePath))).ToArray();
    }

    public static WorkspaceEntry[] GetWorkspaces()
    {
        return WorkspacesCache.GetOrCompute("workspaces", () =>
        {
            var items = new List<WorkspaceEntry>();
            if (File.Exists(ConfigFile))
            {
                try
                {
                    var raw = File.ReadAllText(ConfigFile);
                    var loaded = JsonSerializer.Deserialize<WorkspaceEntry[]>(raw)?
                        .Where(w => !string.IsNullOrEmpty(w.WorkspacePath) && Directory.Exists(w.WorkspacePath))
                        .Select(w => string.IsNullOrEmpty(w.Alias) ? w with { Alias = DeriveAlias(w.Name) } : w)
                        .ToList() ?? new List<WorkspaceEntry>();
                    items.AddRange(loaded);
                }
                catch (Exception) { }
            }

            var discovered = AutoDiscoverWorkspaces();
            bool addedNew = false;
            var existingPaths = new HashSet<string>(items.Select(i => i.WorkspacePath), StringComparer.OrdinalIgnoreCase);

            foreach (var disc in discovered)
            {
                if (existingPaths.Add(disc.WorkspacePath))
                {
                    items.Add(disc);
                    addedNew = true;
                }
            }

            if (addedNew || !File.Exists(ConfigFile))
            {
                SaveWorkspaces(items.ToArray());
            }

            return items.ToArray();
        });
    }

    public static int PruneWorkspaces()
    {
        if (!File.Exists(ConfigFile)) return 0;
        try
        {
            var raw = File.ReadAllText(ConfigFile);
            var items = JsonSerializer.Deserialize<WorkspaceEntry[]>(raw) ?? [];
            var valid = items.Where(w => !string.IsNullOrEmpty(w.WorkspacePath) && Directory.Exists(w.WorkspacePath))
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
                list.Add(new WorkspaceEntry(name, path, "default", ["auto-discovered"], null, alias));
            }
        }

        try
        {
            var currentDir = Directory.GetCurrentDirectory();
            TryAdd(Path.GetFileName(currentDir), currentDir);
        }
        catch { }

        var userProfile = AppPaths.UserProfileDir;
        TryAdd("Powershell Profile", Path.Combine(userProfile, "Documents", "Powershell"));

        var searchBases = new List<string>();

        if (!string.IsNullOrEmpty(Config.Current.Project.BaseDir))
            searchBases.Add(Config.Current.Project.BaseDir);

        if (Config.Current.Project.SearchPaths != null)
        {
            foreach (var sp in Config.Current.Project.SearchPaths)
            {
                if (!string.IsNullOrEmpty(sp)) searchBases.Add(sp);
            }
        }

        var usersParent = Path.GetDirectoryName(userProfile) ?? @"C:\Users";
        if (Directory.Exists(usersParent))
        {
            try
            {
                foreach (var uDir in Directory.GetDirectories(usersParent))
                {
                    searchBases.Add(Path.Combine(uDir, "project"));
                    searchBases.Add(Path.Combine(uDir, "project", "learning"));
                    searchBases.Add(Path.Combine(uDir, "learning"));
                    searchBases.Add(Path.Combine(uDir, "Documents"));
                    searchBases.Add(Path.Combine(uDir, "Desktop"));
                }
            }
            catch { }
        }

        foreach (var baseDir in searchBases.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!Directory.Exists(baseDir)) continue;
            TryScanDirectory(baseDir, 0, 3, TryAdd);
        }

        return list.ToArray();
    }

    private static void TryScanDirectory(string dir, int depth, int maxDepth, Action<string, string> tryAdd)
    {
        if (depth > maxDepth) return;
        try
        {
            var dirName = Path.GetFileName(dir);
            if (dirName.StartsWith(".") || dirName.Equals("node_modules", StringComparison.OrdinalIgnoreCase) || dirName.Equals("bin", StringComparison.OrdinalIgnoreCase) || dirName.Equals("obj", StringComparison.OrdinalIgnoreCase)) return;

            bool isProject = Directory.Exists(Path.Combine(dir, ".git")) ||
                             Directory.GetFiles(dir, "*.csproj").Length > 0 ||
                             Directory.GetFiles(dir, "*.sln").Length > 0 ||
                             File.Exists(Path.Combine(dir, "package.json")) ||
                             File.Exists(Path.Combine(dir, "Cargo.toml")) ||
                             File.Exists(Path.Combine(dir, "go.mod")) ||
                             File.Exists(Path.Combine(dir, "requirements.txt"));

            if (isProject || depth == 0)
            {
                tryAdd(dirName, dir);
            }

            foreach (var sub in Directory.GetDirectories(dir))
            {
                var subName = Path.GetFileName(sub);
                if (subName.StartsWith(".") || subName.Equals("node_modules", StringComparison.OrdinalIgnoreCase) || subName.Equals("bin", StringComparison.OrdinalIgnoreCase) || subName.Equals("obj", StringComparison.OrdinalIgnoreCase)) continue;

                TryScanDirectory(sub, depth + 1, maxDepth, tryAdd);
            }
        }
        catch { }
    }

    public static int SyncAllProjects(string? customBaseDir = null)
    {
        var list = new List<WorkspaceEntry>(GetWorkspaces());
        var discovered = AutoDiscoverWorkspaces();
        var existingPaths = new HashSet<string>(list.Select(i => i.WorkspacePath), StringComparer.OrdinalIgnoreCase);
        int addedCount = 0;

        foreach (var disc in discovered)
        {
            if (existingPaths.Add(disc.WorkspacePath))
            {
                list.Add(disc);
                addedCount++;
            }
        }

        if (addedCount > 0 || !File.Exists(ConfigFile))
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
            WorkspacesCache.Clear();
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

    private static readonly TtlCache<string, string> BranchCache = new(TimeSpan.FromSeconds(5));

    public static string GetGitBranch(string dirPath)
    {
        if (string.IsNullOrEmpty(dirPath)) return "";
        return BranchCache.GetOrCompute(dirPath, () =>
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

    public static readonly string[] SharedWorkspaceActions =
    [
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
    ];

    public static string HandleWorkspaceAction(WorkspaceEntry selected, int actionIdx)
    {
        switch (actionIdx)
        {
            case 0:
                var agyHome = AppPaths.GeminiHome;
                Directory.CreateDirectory(agyHome);
                var selectedProjFile = Path.Combine(agyHome, "selected_project.txt");
                File.WriteAllText(selectedProjFile, selected.WorkspacePath);
                break;
            case 1:
                SystemHelper.OpenNewTerminalSession(selected.WorkspacePath);
                break;
            case 2:
                TerminalIde.Open(selected.WorkspacePath);
                break;
            case 3:
                SystemHelper.OpenExplorer(selected.WorkspacePath);
                break;
            case 4:
                GitDiffViewer.ShowDiff(selected.WorkspacePath);
                break;
            case 5:
                SystemHelper.OpenNewTerminalSession(selected.WorkspacePath, "ask-ai");
                break;
            case 6:
                SystemHelper.OpenNewTerminalSession(selected.WorkspacePath, "cc");
                break;
            case 7:
                var projFiles = Directory.GetFiles(selected.WorkspacePath, "*.csproj", SearchOption.AllDirectories);
                if (projFiles.Length > 0)
                {
                    AnsiConsole.Clear();
                    AnsiConsole.MarkupLine("[bold cyan]🔨 Building .NET Projects...[/]\n");
                    ProcessRunner.Run("dotnet", "build", selected.WorkspacePath);
                    AnsiConsole.MarkupLine("\n[dim]Press any key to return...[/]");
                    Console.ReadKey(true);
                }
                else
                {
                    SpectrePanel.Warning("No C# project (.csproj) found in this workspace.");
                    Thread.Sleep(1500);
                }
                break;
            case 8:
                GitNexus.ShowLiveDashboard();
                break;
            case 9:
                GitNexusStats.Run();
                break;
            case 10:
                ManageWorkspaceLinks(selected);
                break;
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

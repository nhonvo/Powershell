using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Spectre.Console;
using Spectre.Console.Rendering;
using AgyTui.Components;
using AgyTui.Registry;

namespace AgyTui;

public static class SubPageNavigator
{
    private struct FlatItem
    {
        public WorkspaceEntry Workspace;
        public int WorkspaceIndex;
        public int ActionIndex;
    }

    private static string _detailsSearchBuffer = "";
    private static int _selectedWorkspaceIndex = 0;
    private static int _selectedActionIndex = -1;
    private static int _expandedWorkspaceIndex = 0;

    public static void Run(string mode, string initialQuery = "")
    {
        mode = mode.ToLowerInvariant();
        int detailsSel = 0;

        // Initialize selection index if active theme/account exists
        if (mode == "agyswitch")
        {
            var accs = AgyAccountCore.GetAccounts();
            var activeAcc = AgyAccountCore.GetActiveAccount();
            detailsSel = Array.IndexOf(accs, activeAcc);
            if (detailsSel < 0) detailsSel = 0;
        }
        else if (mode == "theme")
        {
            var themeFiles = GetThemeNames();
            var currentTheme = Environment.GetEnvironmentVariable("THEME");
            detailsSel = Array.IndexOf(themeFiles, currentTheme);
            if (detailsSel < 0) detailsSel = 0;
        }
        else if (mode == "proj")
        {
            _selectedWorkspaceIndex = 0;
            _selectedActionIndex = -1;
            _expandedWorkspaceIndex = 0;
        }

        _detailsSearchBuffer = initialQuery;

        while (true)
        {
            int itemsCount = 0;
            if (mode == "agyswitch")
            {
                var accs = AgyAccountCore.GetAccounts();
                if (!string.IsNullOrEmpty(_detailsSearchBuffer))
                {
                    accs = accs.Where(a => a.Contains(_detailsSearchBuffer, StringComparison.OrdinalIgnoreCase)).ToArray();
                }
                itemsCount = accs.Length;
            }
            else if (mode == "theme")
            {
                var themes = GetThemeNames();
                if (!string.IsNullOrEmpty(_detailsSearchBuffer))
                {
                    themes = themes.Where(t => t.Contains(_detailsSearchBuffer, StringComparison.OrdinalIgnoreCase)).ToArray();
                }
                itemsCount = themes.Length;
            }
            else if (mode == "learn" || mode == "session" || mode == "weak")
            {
                var topics = new[] { "jp", "en", "cs", "dsa", "interview", "[Type Custom Topic...]" };
                if (!string.IsNullOrEmpty(_detailsSearchBuffer))
                {
                    topics = topics.Where(t => t.Contains(_detailsSearchBuffer, StringComparison.OrdinalIgnoreCase)).ToArray();
                }
                itemsCount = topics.Length;
            }
            else if (mode == "proj")
            {
                var workspaces = WorkspaceRegistry.GetWorkspaces();
                if (!string.IsNullOrEmpty(_detailsSearchBuffer))
                {
                    workspaces = workspaces.Where(w => w != null && 
                        ((w.Name != null && w.Name.Contains(_detailsSearchBuffer, StringComparison.OrdinalIgnoreCase)) ||
                         (w.WorkspacePath != null && w.WorkspacePath.Contains(_detailsSearchBuffer, StringComparison.OrdinalIgnoreCase)))).ToArray();
                }
                
                var flatList = new List<FlatItem>();
                for (int i = 0; i < workspaces.Length; i++)
                {
                    var w = workspaces[i];
                    flatList.Add(new FlatItem { Workspace = w, WorkspaceIndex = i, ActionIndex = -1 });
                    if (i == _expandedWorkspaceIndex)
                    {
                        for (int j = 0; j < WorkspaceRegistry.SharedWorkspaceActions.Length; j++)
                        {
                            flatList.Add(new FlatItem { Workspace = w, WorkspaceIndex = i, ActionIndex = j });
                        }
                    }
                }
                itemsCount = flatList.Count;
                
                detailsSel = flatList.FindIndex(item => item.WorkspaceIndex == _selectedWorkspaceIndex && item.ActionIndex == _selectedActionIndex);
                if (detailsSel < 0)
                {
                    if (flatList.Count > 0)
                    {
                        detailsSel = 0;
                        _selectedWorkspaceIndex = flatList[0].WorkspaceIndex;
                        _selectedActionIndex = flatList[0].ActionIndex;
                    }
                    else
                    {
                        detailsSel = 0;
                        _selectedWorkspaceIndex = 0;
                        _selectedActionIndex = -1;
                    }
                }
            }

            ScreenChrome.RenderFrame(() =>
            {
                RenderSubPageSelection(mode, detailsSel);
            }, forceClear: true);

            var key = Console.ReadKey(true);

            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                case ConsoleKey.K:
                    if (itemsCount > 0) detailsSel = (detailsSel - 1 + itemsCount) % itemsCount;
                    break;
                case ConsoleKey.DownArrow:
                case ConsoleKey.J:
                    if (itemsCount > 0) detailsSel = (detailsSel + 1) % itemsCount;
                    break;
                case ConsoleKey.PageUp:
                    if (itemsCount > 0) detailsSel = Math.Max(0, detailsSel - ScrollableListView.GetPageStep(Math.Max(3, Console.WindowHeight - 19)));
                    break;
                case ConsoleKey.PageDown:
                    if (itemsCount > 0) detailsSel = Math.Min(itemsCount - 1, detailsSel + ScrollableListView.GetPageStep(Math.Max(3, Console.WindowHeight - 19)));
                    break;
                case ConsoleKey.Home:
                    detailsSel = 0;
                    break;
                case ConsoleKey.End:
                    if (itemsCount > 0) detailsSel = Math.Max(0, itemsCount - 1);
                    break;
                case ConsoleKey.Backspace:
                    if (!string.IsNullOrEmpty(_detailsSearchBuffer))
                    {
                        if (key.Modifiers.HasFlag(ConsoleModifiers.Control) || key.KeyChar == '\x17' || key.KeyChar == '\x7f' || key.KeyChar == '\x08')
                        {
                            _detailsSearchBuffer = DeletePreviousWord(_detailsSearchBuffer);
                        }
                        else
                        {
                            _detailsSearchBuffer = _detailsSearchBuffer[..^1];
                        }
                        detailsSel = 0;
                        if (mode == "proj")
                        {
                            _selectedWorkspaceIndex = 0;
                            _selectedActionIndex = -1;
                            _expandedWorkspaceIndex = 0;
                        }
                    }
                    break;
                case ConsoleKey.Enter:
                    if (mode == "proj")
                    {
                        var workspaces = WorkspaceRegistry.GetWorkspaces();
                        if (!string.IsNullOrEmpty(_detailsSearchBuffer))
                        {
                            workspaces = workspaces.Where(w => w != null && 
                                ((w.Name != null && w.Name.Contains(_detailsSearchBuffer, StringComparison.OrdinalIgnoreCase)) ||
                                 (w.WorkspacePath != null && w.WorkspacePath.Contains(_detailsSearchBuffer, StringComparison.OrdinalIgnoreCase)))).ToArray();
                        }
                        var flatList = new List<FlatItem>();
                        for (int i = 0; i < workspaces.Length; i++)
                        {
                            var w = workspaces[i];
                            flatList.Add(new FlatItem { Workspace = w, WorkspaceIndex = i, ActionIndex = -1 });
                            if (i == _expandedWorkspaceIndex)
                            {
                                for (int j = 0; j < WorkspaceRegistry.SharedWorkspaceActions.Length; j++)
                                {
                                    flatList.Add(new FlatItem { Workspace = w, WorkspaceIndex = i, ActionIndex = j });
                                }
                            }
                        }

                        if (detailsSel >= 0 && detailsSel < flatList.Count)
                        {
                            var item = flatList[detailsSel];
                            if (item.ActionIndex == -1)
                            {
                                if (_expandedWorkspaceIndex == item.WorkspaceIndex)
                                {
                                    _expandedWorkspaceIndex = -1;
                                }
                                else
                                {
                                    _expandedWorkspaceIndex = item.WorkspaceIndex;
                                }
                            }
                            else
                            {
                                WorkspaceRegistry.HandleWorkspaceAction(item.Workspace, item.ActionIndex);
                                return;
                            }
                        }
                    }
                    else
                    {
                        if (detailsSel >= 0 && detailsSel < itemsCount)
                        {
                            bool shouldExit = HandleSelection(mode, detailsSel);
                            if (shouldExit) return;
                        }
                    }
                    break;
                case ConsoleKey.A:
                    if (mode == "agyswitch")
                    {
                        CreateAccount();
                    }
                    else
                    {
                        _detailsSearchBuffer += key.KeyChar;
                        detailsSel = 0;
                        if (mode == "proj")
                        {
                            _selectedWorkspaceIndex = 0;
                            _selectedActionIndex = -1;
                            _expandedWorkspaceIndex = 0;
                        }
                    }
                    break;
                case ConsoleKey.D:
                    if (mode == "agyswitch" && detailsSel >= 0 && detailsSel < itemsCount)
                    {
                        DeleteAccount(detailsSel);
                    }
                    else
                    {
                        _detailsSearchBuffer += key.KeyChar;
                        detailsSel = 0;
                        if (mode == "proj")
                        {
                            _selectedWorkspaceIndex = 0;
                            _selectedActionIndex = -1;
                            _expandedWorkspaceIndex = 0;
                        }
                    }
                    break;
                case ConsoleKey.O:
                    if (mode == "agyswitch" && detailsSel >= 0 && detailsSel < itemsCount)
                    {
                        LogoutAccount(detailsSel);
                    }
                    else
                    {
                        _detailsSearchBuffer += key.KeyChar;
                        detailsSel = 0;
                        if (mode == "proj")
                        {
                            _selectedWorkspaceIndex = 0;
                            _selectedActionIndex = -1;
                            _expandedWorkspaceIndex = 0;
                        }
                    }
                    break;
                case ConsoleKey.LeftArrow:
                case ConsoleKey.Escape:
                case ConsoleKey.Q:
                    if (!string.IsNullOrEmpty(_detailsSearchBuffer))
                    {
                        _detailsSearchBuffer = "";
                        detailsSel = 0;
                        if (mode == "proj")
                        {
                            _selectedWorkspaceIndex = 0;
                            _selectedActionIndex = -1;
                            _expandedWorkspaceIndex = 0;
                        }
                    }
                    else
                    {
                        return;
                    }
                    break;
                default:
                    if (key.KeyChar >= 32 && key.KeyChar <= 126 && key.Key != ConsoleKey.Enter)
                    {
                        if (mode == "agyswitch" && (key.Key == ConsoleKey.A || key.Key == ConsoleKey.D || key.Key == ConsoleKey.O) && string.IsNullOrEmpty(_detailsSearchBuffer))
                        {
                            break;
                        }
                        _detailsSearchBuffer += key.KeyChar;
                        detailsSel = 0;
                        if (mode == "proj")
                        {
                            _selectedWorkspaceIndex = 0;
                            _selectedActionIndex = -1;
                            _expandedWorkspaceIndex = 0;
                        }
                    }
                    break;
            }

            // Sync selection state back from detailsSel
            if (mode == "proj" && itemsCount > 0)
            {
                var workspaces = WorkspaceRegistry.GetWorkspaces();
                if (!string.IsNullOrEmpty(_detailsSearchBuffer))
                {
                    workspaces = workspaces.Where(w => w != null && 
                        ((w.Name != null && w.Name.Contains(_detailsSearchBuffer, StringComparison.OrdinalIgnoreCase)) ||
                         (w.WorkspacePath != null && w.WorkspacePath.Contains(_detailsSearchBuffer, StringComparison.OrdinalIgnoreCase)))).ToArray();
                }
                var flatList = new List<FlatItem>();
                for (int i = 0; i < workspaces.Length; i++)
                {
                    var w = workspaces[i];
                    flatList.Add(new FlatItem { Workspace = w, WorkspaceIndex = i, ActionIndex = -1 });
                    if (i == _expandedWorkspaceIndex)
                    {
                        for (int j = 0; j < WorkspaceRegistry.SharedWorkspaceActions.Length; j++)
                        {
                            flatList.Add(new FlatItem { Workspace = w, WorkspaceIndex = i, ActionIndex = j });
                        }
                    }
                }

                if (detailsSel < 0) detailsSel = 0;
                if (detailsSel >= flatList.Count) detailsSel = flatList.Count - 1;
                
                if (flatList.Count > 0)
                {
                    var selectedItem = flatList[detailsSel];
                    _selectedWorkspaceIndex = selectedItem.WorkspaceIndex;
                    _selectedActionIndex = selectedItem.ActionIndex;
                }
            }
        }
    }

    private static string DeletePreviousWord(string buffer)
    {
        if (string.IsNullOrEmpty(buffer)) return "";
        int i = buffer.Length - 1;
        while (i >= 0 && char.IsWhiteSpace(buffer[i])) i--;
        while (i >= 0 && !char.IsWhiteSpace(buffer[i])) i--;
        return buffer[..(i + 1)];
    }

    private static string[] GetThemeNames()
    {
        var themesPath = Environment.GetEnvironmentVariable("POSH_THEMES_PATH");
        if (string.IsNullOrEmpty(themesPath) || !Directory.Exists(themesPath))
        {
            themesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "asset", "powershell-themes");
            if (!Directory.Exists(themesPath))
            {
                themesPath = Path.Combine(Directory.GetCurrentDirectory(), "asset", "powershell-themes");
            }
        }
        if (!Directory.Exists(themesPath)) return Array.Empty<string>();
        return Directory.GetFiles(themesPath, "*.omp.json").Select(f => Path.GetFileName(f).Replace(".omp.json", "")).OrderBy(f => f).ToArray();
    }

    private static bool HandleSelection(string mode, int detailsSel)
    {
        if (mode == "agyswitch")
        {
            var accs = AgyAccountCore.GetAccounts();
            if (!string.IsNullOrEmpty(_detailsSearchBuffer))
            {
                accs = accs.Where(a => a.Contains(_detailsSearchBuffer, StringComparison.OrdinalIgnoreCase)).ToArray();
            }
            var targetAcc = accs[detailsSel];
            Console.CursorVisible = true;
            AgyAccountCore.SetActiveAccount(targetAcc, false);
            Console.CursorVisible = false;
            return true;
        }
        else if (mode == "theme")
        {
            var themeNames = GetThemeNames();
            if (!string.IsNullOrEmpty(_detailsSearchBuffer))
            {
                themeNames = themeNames.Where(t => t.Contains(_detailsSearchBuffer, StringComparison.OrdinalIgnoreCase)).ToArray();
            }
            var selectedTheme = themeNames[detailsSel];
            var themesPath = Environment.GetEnvironmentVariable("POSH_THEMES_PATH");
            if (string.IsNullOrEmpty(themesPath))
            {
                themesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "asset", "powershell-themes");
                if (!Directory.Exists(themesPath))
                {
                    themesPath = Path.Combine(Directory.GetCurrentDirectory(), "asset", "powershell-themes");
                }
            }
            var configPath = Path.Combine(themesPath, "config.json");
            try
            {
                File.WriteAllText(configPath, JsonSerializer.Serialize(new { active_theme = selectedTheme, enable_mobile = selectedTheme.EndsWith("-mobile") }));
            }
            catch { }
            Environment.SetEnvironmentVariable("THEME", selectedTheme);
            var themePath = Path.Combine(themesPath, $"{selectedTheme}.omp.json");
            var agyHome = !string.IsNullOrEmpty(AgyAccountCore.AgySourceHome) ? AgyAccountCore.AgySourceHome : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".gemini");
            Directory.CreateDirectory(agyHome);
            var selectedThemeFile = Path.Combine(agyHome, "selected_theme.txt");
            File.WriteAllText(selectedThemeFile, themePath);
            SpectrePanel.Success($"Selected theme '{selectedTheme}'. Theme will apply on exit.");
            Thread.Sleep(1000);
            return true;
        }
        else if (mode == "learn" || mode == "session" || mode == "weak")
        {
            var topics = new[] { "jp", "en", "cs", "dsa", "interview", "[Type Custom Topic...]" };
            if (!string.IsNullOrEmpty(_detailsSearchBuffer))
            {
                topics = topics.Where(t => t.Contains(_detailsSearchBuffer, StringComparison.OrdinalIgnoreCase)).ToArray();
            }
            var selectedTopic = topics[detailsSel];
            if (selectedTopic == "[Type Custom Topic...]")
            {
                Console.CursorVisible = true;
                selectedTopic = AnsiConsole.Ask<string>("Enter custom topic name:").Trim();
                Console.CursorVisible = false;
            }
            if (!string.IsNullOrEmpty(selectedTopic))
            {
                Console.CursorVisible = true;
                if (mode == "learn") LearnRouter.StartLearning(selectedTopic);
                else if (mode == "session") StudySession.Run(selectedTopic);
                else if (mode == "weak") WeakItemsQueue.ShowPreSessionReview(selectedTopic);
                Console.CursorVisible = false;
            }
            return true;
        }
        else if (mode == "proj")
        {
            var workspaces = WorkspaceRegistry.GetWorkspaces();
            if (!string.IsNullOrEmpty(_detailsSearchBuffer))
            {
                workspaces = workspaces.Where(w => w != null && 
                    ((w.Name != null && w.Name.Contains(_detailsSearchBuffer, StringComparison.OrdinalIgnoreCase)) ||
                     (w.WorkspacePath != null && w.WorkspacePath.Contains(_detailsSearchBuffer, StringComparison.OrdinalIgnoreCase)))).ToArray();
            }
            if (detailsSel >= 0 && detailsSel < workspaces.Length)
            {
                var targetEntry = workspaces[detailsSel];
                var actionIdx = SpectreMenu.ShowWithEscape($"Workspace: {targetEntry.Name}", WorkspaceRegistry.SharedWorkspaceActions, 0);
                if (actionIdx >= 0)
                {
                    WorkspaceRegistry.HandleWorkspaceAction(targetEntry, actionIdx);
                }
            }
            return true;
        }
        return false;
    }

    private static void CreateAccount()
    {
        Console.CursorVisible = true;
        AnsiConsole.Clear();
        var newName = AnsiConsole.Ask<string>("Enter new account name:").Trim();
        if (!string.IsNullOrEmpty(newName))
        {
            try
            {
                AgyAccountCore.AddAccount(newName);
                SpectrePanel.Success($"Account '{newName}' created successfully!");
                Thread.Sleep(1500);
            }
            catch (Exception ex)
            {
                SpectrePanel.Error($"Failed to create account: {ex.Message}");
                Thread.Sleep(2000);
            }
        }
        Console.CursorVisible = false;
    }

    private static void DeleteAccount(int detailsSel)
    {
        var accs = AgyAccountCore.GetAccounts();
        if (!string.IsNullOrEmpty(_detailsSearchBuffer))
        {
            accs = accs.Where(a => a.Contains(_detailsSearchBuffer, StringComparison.OrdinalIgnoreCase)).ToArray();
        }
        var targetAcc = accs[detailsSel];
        if (string.Equals(targetAcc, "default", StringComparison.OrdinalIgnoreCase))
        {
            SpectrePanel.Error("Cannot delete default account.");
            Thread.Sleep(1500);
            return;
        }
        Console.CursorVisible = true;
        AnsiConsole.Clear();
        var confirm = AnsiConsole.Confirm($"Are you sure you want to delete account '{targetAcc}'?");
        if (confirm)
        {
            AgyAccountCore.DeleteAccount(targetAcc);
            SpectrePanel.Success($"Account '{targetAcc}' deleted successfully!");
            Thread.Sleep(1500);
        }
        Console.CursorVisible = false;
    }

    private static void LogoutAccount(int detailsSel)
    {
        var accs = AgyAccountCore.GetAccounts();
        if (!string.IsNullOrEmpty(_detailsSearchBuffer))
        {
            accs = accs.Where(a => a.Contains(_detailsSearchBuffer, StringComparison.OrdinalIgnoreCase)).ToArray();
        }
        var targetAcc = accs[detailsSel];
        Console.CursorVisible = true;
        AnsiConsole.Clear();
        var confirm = AnsiConsole.Confirm($"Are you sure you want to log out of '{targetAcc}'?");
        if (confirm)
        {
            AgyAccountCore.LogoutAccount(targetAcc);
            SpectrePanel.Success($"Logged out of '{targetAcc}' successfully!");
            Thread.Sleep(1500);
        }
        Console.CursorVisible = false;
    }

    private static void RenderSubPageSelection(string mode, int selIdx)
    {
        var grid = new Grid();
        grid.AddColumn(new GridColumn().NoWrap());

        IRenderable content = grid;

        if (mode == "agyswitch")
        {
            grid.AddRow(new Markup("[cyan bold]Select Account to Switch:[/]\n"));
            if (!string.IsNullOrEmpty(_detailsSearchBuffer))
            {
                grid.AddRow(new Markup($"[yellow]Search:[/] [white]{_detailsSearchBuffer.EscapeMarkup()}[/]_\n"));
            }
            var allAccs = AgyAccountCore.GetAccounts();
            var accs = string.IsNullOrEmpty(_detailsSearchBuffer)
                ? allAccs
                : allAccs.Where(a => a.Contains(_detailsSearchBuffer, StringComparison.OrdinalIgnoreCase)).ToArray();
            var activeAcc = AgyAccountCore.GetActiveAccount();
            for (var i = 0; i < accs.Length; i++)
            {
                var isSelected = (i == selIdx);
                var isActive = (accs[i] == activeAcc);
                var prefix = isSelected ? "[green bold]> [/]" : "  ";
                var suffix = isActive ? " [green](Active)[/]" : "";
                var displayName = accs[i];
                if (string.Equals(accs[i], "default", StringComparison.OrdinalIgnoreCase))
                {
                    var email = AgyAccountCore.GetAccountEmail("default");
                    if (!string.IsNullOrEmpty(email)) displayName = $"default ({email})";
                }
                var stats = AgyAccountCore.GetAccountStats(accs[i]);
                var loginStatus = stats.TokenStatus == "Logged In" ? "[green]✔[/]" : "[red]✘[/]";
                grid.AddRow(new Markup($"{prefix}{displayName.EscapeMarkup()} [dim]({loginStatus})[/]{suffix}"));
            }
            grid.AddRow(new Markup("\n[dim]↑/↓ Navigate  ·  Enter Select  ·  Esc Cancel[/]"));
            grid.AddRow(new Markup("[dim]a Create Account  ·  d Delete  ·  o Log Out[/]"));
        }
        else if (mode == "theme")
        {
            var themeNames = GetThemeNames();
            var filtered = string.IsNullOrEmpty(_detailsSearchBuffer)
                ? themeNames
                : themeNames.Where(t => t.Contains(_detailsSearchBuffer, StringComparison.OrdinalIgnoreCase)).ToArray();

            var currentTheme = Environment.GetEnvironmentVariable("THEME");

            grid.AddRow(new Markup($"[cyan bold]Select Oh My Posh Theme[/] [dim]({filtered.Length}/{themeNames.Length} themes)[/]:\n"));
            if (!string.IsNullOrEmpty(_detailsSearchBuffer))
            {
                grid.AddRow(new Markup($"[yellow]Search:[/] [white]{_detailsSearchBuffer.EscapeMarkup()}[/]_\n"));
            }
            else
            {
                grid.AddRow(new Markup("[dim]Type to filter themes (Esc to clear / cancel)[/]\n"));
            }

            int maxRows = 12;
            int topRow = 0;
            int endRow = 0;

            if (filtered.Length == 0)
            {
                grid.AddRow(new Markup($"  [dim]No themes matching '{_detailsSearchBuffer.EscapeMarkup()}'.[/]"));
            }
            else
            {
                (topRow, endRow) = ScrollableListView.ComputeViewport(filtered.Length, selIdx, maxRows);

                for (var i = topRow; i < endRow; i++)
                {
                    var isSelected = (i == selIdx);
                    var isActive = string.Equals(filtered[i], currentTheme, StringComparison.OrdinalIgnoreCase);
                    var prefix = isSelected ? "[green bold]> [/]" : "  ";
                    var suffix = isActive ? " [bold green][[ACTIVE]][/]" : "";
                    var nameMarkup = isSelected ? $"[bold green]{filtered[i].EscapeMarkup()}[/]" : $"[white]{filtered[i].EscapeMarkup()}[/]";
                    grid.AddRow(new Markup($"{prefix}{nameMarkup}{suffix}"));
                }
            }

            string scrollStatus = "";
            if (filtered.Length > maxRows)
            {
                var aboveStr = topRow > 0 ? $"[yellow]▲ {topRow} items above[/]" : "[grey]▲ Start of list[/]";
                var belowStr = (endRow < filtered.Length) ? $"[yellow]▼ {filtered.Length - endRow} items below[/]" : "[grey]▼ End of list[/]";
                scrollStatus = $"  {aboveStr}   ·   {belowStr}";
            }
            else
            {
                scrollStatus = "  [grey]▲ Start of list   ·   ▼ End of list[/]";
            }

            content = new Rows(
                grid,
                new Rule().RuleStyle("cyan dim"),
                new Markup(scrollStatus),
                new Markup("\n[dim]↑/↓/j/k Navigate  ·  PgDn/PgUp Page  ·  Enter Select  ·  Esc Cancel[/]")
            );
        }
        else if (mode == "learn" || mode == "session" || mode == "weak")
        {
            grid.AddRow(new Markup($"[cyan bold]Select Topic for {mode.ToUpperInvariant()}:[/]\n"));
            if (!string.IsNullOrEmpty(_detailsSearchBuffer))
            {
                grid.AddRow(new Markup($"[yellow]Search:[/] [white]{_detailsSearchBuffer.EscapeMarkup()}[/]_\n"));
            }
            var allTopics = new[] { "jp (Japanese / Language)", "en (English Vocabulary)", "cs (C# Quiz)", "dsa (Data Structures & Algorithms)", "interview (Question Bank & STAR)", "[Type Custom Topic...]" };
            var topics = string.IsNullOrEmpty(_detailsSearchBuffer)
                ? allTopics
                : allTopics.Where(t => t.Contains(_detailsSearchBuffer, StringComparison.OrdinalIgnoreCase)).ToArray();
            for (var i = 0; i < topics.Length; i++)
            {
                var isSelected = (i == selIdx);
                var prefix = isSelected ? "[green bold]> [/]" : "  ";
                grid.AddRow(new Markup($"{prefix}{topics[i].EscapeMarkup()}"));
            }
            grid.AddRow(new Markup("\n[dim]↑/↓ Navigate  ·  Enter Select  ·  Esc Cancel[/]"));
        }
        else if (mode == "proj")
        {
            var allWorkspaces = WorkspaceRegistry.GetWorkspaces();
            var workspaces = string.IsNullOrEmpty(_detailsSearchBuffer)
                ? allWorkspaces
                : allWorkspaces.Where(w => w != null && 
                    ((w.Name != null && w.Name.Contains(_detailsSearchBuffer, StringComparison.OrdinalIgnoreCase)) ||
                     (w.WorkspacePath != null && w.WorkspacePath.Contains(_detailsSearchBuffer, StringComparison.OrdinalIgnoreCase)))).ToArray();

            var currentDir = Directory.GetCurrentDirectory();

            grid.AddRow(new Markup($"[bold green]📁 Registered Workspace Navigator (cnav)[/] [dim]({workspaces.Length}/{allWorkspaces.Length} workspaces)[/]:\n"));

            if (!string.IsNullOrEmpty(_detailsSearchBuffer))
            {
                grid.AddRow(new Markup($"[yellow]Search:[/] [white]{_detailsSearchBuffer.EscapeMarkup()}[/]_\n"));
            }
            else
            {
                grid.AddRow(new Markup("[dim]Type to filter workspaces (Esc to clear / cancel)[/]\n"));
            }

            if (workspaces.Length == 0)
            {
                grid.AddRow(new Markup($"  [dim]No workspaces matching '{_detailsSearchBuffer.EscapeMarkup()}'.[/]"));
            }
            else
            {
                var flatList = new List<FlatItem>();
                for (int i = 0; i < workspaces.Length; i++)
                {
                    var w = workspaces[i];
                    flatList.Add(new FlatItem { Workspace = w, WorkspaceIndex = i, ActionIndex = -1 });
                    if (i == _expandedWorkspaceIndex)
                    {
                        for (int j = 0; j < WorkspaceRegistry.SharedWorkspaceActions.Length; j++)
                        {
                            flatList.Add(new FlatItem { Workspace = w, WorkspaceIndex = i, ActionIndex = j });
                        }
                    }
                }

                int termH = 30;
                try { termH = Console.WindowHeight; } catch { }
                int maxRows = Math.Max(5, termH - 18);
                int topRow = 0;
                int endRow = 0;

                (topRow, endRow) = ScrollableListView.ComputeViewport(flatList.Count, selIdx, maxRows);

                for (int i = topRow; i < endRow; i++)
                {
                    var item = flatList[i];
                    var isSelected = (i == selIdx);

                    if (item.ActionIndex == -1)
                    {
                        var ws = item.Workspace;
                        var isCurrent = !string.IsNullOrEmpty(ws.WorkspacePath) && string.Equals(ws.WorkspacePath.TrimEnd('\\', '/'), currentDir.TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase);

                        var prefix = isSelected ? "[green bold]❯ [/]" : "  ";
                        var status = isCurrent ? "[bold black on green] ACTIVE [/] " : "";
                        var branch = WorkspaceRegistry.GetGitBranch(ws.WorkspacePath);
                        var branchSuffix = !string.IsNullOrEmpty(branch) ? $" [yellow]🌿 {branch}[/]" : "";
                        var nameMarkup = isSelected ? $"[bold green]{ws.Name.EscapeMarkup()}[/]" : $"[bold white]{ws.Name.EscapeMarkup()}[/]";
                        var pathMarkup = $"[dim]· {ws.WorkspacePath.EscapeMarkup()}[/]";

                        var expandSign = (item.WorkspaceIndex == _expandedWorkspaceIndex) ? "[-] " : "[+] ";

                        grid.AddRow(new Markup($"{prefix}{expandSign}📁 {status}{nameMarkup}{branchSuffix} {pathMarkup}"));
                    }
                    else
                    {
                        var isLast = (item.ActionIndex == WorkspaceRegistry.SharedWorkspaceActions.Length - 1);
                        var bullet = isLast ? "└── " : "├── ";
                        var prefix = isSelected ? "  [green bold]❯──[/] " : $"  {bullet}";

                        var actionLabel = WorkspaceRegistry.SharedWorkspaceActions[item.ActionIndex];
                        var labelMarkup = isSelected ? $"[bold green]{actionLabel.EscapeMarkup()}[/]" : $"[dim]{actionLabel.EscapeMarkup()}[/]";
                        grid.AddRow(new Markup($"{prefix}{labelMarkup}"));
                    }
                }

                string scrollStatus = "";
                if (flatList.Count > maxRows)
                {
                    var aboveStr = topRow > 0 ? $"[yellow]▲ {topRow} items above[/]" : "[grey]▲ Start of list[/]";
                    var belowStr = (endRow < flatList.Count) ? $"[yellow]▼ {flatList.Count - endRow} items below[/]" : "[grey]▼ End of list[/]";
                    scrollStatus = $"  {aboveStr}   ·   {belowStr}";
                }
                else
                {
                    scrollStatus = "  [grey]▲ Start of list   ·   ▼ End of list[/]";
                }

                var selTarget = (_selectedWorkspaceIndex >= 0 && _selectedWorkspaceIndex < workspaces.Length) ? workspaces[_selectedWorkspaceIndex]?.WorkspacePath : null;
                var targetDisplay = !string.IsNullOrEmpty(selTarget) ? selTarget : "No workspace selected";

                content = new Rows(
                    grid,
                    new Rule().RuleStyle("cyan dim"),
                    new Markup(scrollStatus),
                    new Markup($"  [dim]Selected Target:[/] [bold cyan]{targetDisplay.EscapeMarkup()}[/]"),
                    new Markup("\n[bold cyan][[Enter]][/] Toggle/Run  ·  [bold cyan][[Esc]][/] Cancel")
                );
            }
        }

        var panel = new Panel(content)
        {
            Header = new PanelHeader($"[bold cyan] {mode.ToUpperInvariant()} Selector [/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Cyan1),
            Expand = true
        };
        ScreenChrome.WriteSmooth(panel);
    }
}

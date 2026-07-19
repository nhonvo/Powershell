using System;

using System.Buffers;

using System.Collections.Frozen;

using System.Collections.Generic;

using System.Diagnostics;

using System.IO;

using System.Linq;

using System.Net;

using System.Net.Http;

using System.Net.NetworkInformation;

using System.Net.Sockets;

using System.Runtime.InteropServices;

using System.Security.AccessControl;

using System.Security.Cryptography;

using System.Text;

using System.Text.Json;

using System.Text.Json.Serialization;

using System.Text.RegularExpressions;

using System.Threading;

using Spectre.Console;

namespace AgyTui;

using AgyTui.Components;


public sealed record PaletteCommand(string Alias, string Description, string Category);

public static class CommandPalette
{
    public static readonly PaletteCommand[] Commands = AgyTui.Registry.CommandRegistry.All
        .Select(c => new PaletteCommand(c.Alias, c.Description, c.HelpCategory))
        .ToArray();

    public static void Show()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Rule("[bold cyan]Command Palette[/]").RuleStyle("grey"));
        var categories=Commands.Select(c => c.Category).Distinct().ToArray();
        var catIdx=SpectreMenu.Show("Category", ["All",..categories], 0, true);
        IEnumerable<PaletteCommand>filtered=catIdx<=0?Commands:Commands.Where(c => c.Category==categories[catIdx-1]);
        var items=filtered.Select(c => $"{c.Alias,-20} {c.Description}").ToArray();
        var cmds=filtered.Select(c => c.Alias).ToArray();
        var selected=SpectreMenu.Show(["Command Palette","Select a command to view details"], items, cmds, 0, true, false);
        if (selected>=0)
        {
            var cmd=filtered.ElementAt(selected);
            AnsiConsole.Write(new Panel($"[bold]Alias:[/] {cmd.Alias.EscapeMarkup()}\n"+$"[bold]Category:[/] {cmd.Category.EscapeMarkup()}\n"+$"[bold]Description:[/] {cmd.Description.EscapeMarkup()}")
            {
                Header=new PanelHeader("[bold cyan]Command Details[/]"), Border=BoxBorder.Rounded
            }
            );
        }

    }

}
public static class ProfileHelp
{
    private static readonly FrozenDictionary<string, string[]> HelpTopics = AgyTui.Registry.CommandRegistry.All
        .Where(c => c.HelpLines.Length > 0)
        .GroupBy(c => c.HelpCategory)
        .ToDictionary(
            g => g.Key,
            g => g.SelectMany(c => c.HelpLines).ToArray(),
            StringComparer.OrdinalIgnoreCase
        )
        .ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    public static void Show()
    {
        AnsiConsole.Write(new Rule("[bold cyan]Help Browser[/]").RuleStyle("grey"));
        var topics=HelpTopics.Keys.ToArray();
        var idx=SpectreMenu.Show("Help Topics", topics, 0, true);
        if (idx<0)return;
        var topic=topics[idx];
        SpectrePager.Show($"Help: {topic}", HelpTopics[topic]);

    }

    public static Dictionary<string, Dictionary<string, CommandDoc[]>>GetCommands(string jsonPath)
    {
        if (!File.Exists(jsonPath))return new();

        try
        {
            var raw=File.ReadAllText(jsonPath);
            var opts=new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive=true
            }
            ;
            return JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, CommandDoc[]>>>(raw, opts)??new();
        }
        catch
        {
            return new();
        }

    }

    public static CommandDoc?ShowInteractive(string jsonPath, string initialFilter)
    {
        var cmdsNested=GetCommands(jsonPath);
        var cmds=new Dictionary<string, CommandDoc[]>();
        foreach (var(_, subDict)in cmdsNested)foreach (var(sub, docs)in subDict)cmds[sub]=docs;
        var categories=cmds.Keys.OrderBy(k => k, StringComparer.Ordinal).ToArray();
        var allCommands=categories.SelectMany(c => cmds[c]).ToArray();
        var categoryLookup=new Dictionary<string, string>();
        foreach (var c in categories)categoryLookup[$"{c} ({cmds[c].Length} commands)"]=c;
        var commandLookup=new Dictionary<string, CommandDoc>();
        foreach (var c in allCommands)commandLookup[$"{c.Alias,-10} - {c.Desc}"]=c;
        string[]TopResolver(string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))return categories.Select(c => $"{c} ({cmds[c].Length} commands)").ToArray();
            return allCommands.Where(c => c.Alias.Contains(filter, StringComparison.OrdinalIgnoreCase)||c.Desc.Contains(filter, StringComparison.OrdinalIgnoreCase)||c.Command.Contains(filter, StringComparison.OrdinalIgnoreCase)).Select(c => $"{c.Alias,-10} - {c.Desc}").ToArray();
        }
        var filter=initialFilter;
        while (true)
        {
            var selectedLabel=SpectreMenu.ShowDynamic("Select Help Category", TopResolver, 0, filter);
            filter="";
            if (selectedLabel==null)return null;
            if (commandLookup.TryGetValue(selectedLabel, out var cmdObj))return cmdObj;
            if (categoryLookup.TryGetValue(selectedLabel, out var catName))
            {
                var catCmds=cmds[catName];
                var subLookup=new Dictionary<string, CommandDoc>();
                foreach (var c in catCmds)subLookup[$"{c.Alias,-10} - {c.Desc}"]=c;
                string[]SubResolver(string subFilter) => catCmds.Where(c => string.IsNullOrWhiteSpace(subFilter)||c.Alias.Contains(subFilter, StringComparison.OrdinalIgnoreCase)||c.Desc.Contains(subFilter, StringComparison.OrdinalIgnoreCase)||c.Command.Contains(subFilter, StringComparison.OrdinalIgnoreCase)).Select(c => $"{c.Alias,-10} - {c.Desc}").ToArray();
                while (true)
                {
                    var selectedSubLabel=SpectreMenu.ShowDynamic($"Category: {catName}", SubResolver, 0);
                    if (selectedSubLabel==null)break;
                    if (subLookup.TryGetValue(selectedSubLabel, out var subCmd))return subCmd;
                }
            }
        }

    }

}

public sealed record CommandDoc(string Alias, string FullName, string Desc, string Command);

public static class AgyHeader
{
    public static void ShowSplash()
    {
        AnsiConsole.Clear();
        var splashW=Math.Min(65, Math.Max(50, Console.WindowWidth-2));
        var sep=new string('=', splashW);
        AnsiConsole.MarkupLine($"[cyan]{sep.EscapeMarkup()}[/]");
        AnsiConsole.Write(new FigletText("AGY TUI").Centered().Color(Color.Green));
        AnsiConsole.Write(new Rule("[bold green]🛸 Powershell Profile Control Center v3.0 🛸[/]").RuleStyle("grey"));
        AnsiConsole.MarkupLine($"[cyan]{sep.EscapeMarkup()}[/]");
        AnsiConsole.WriteLine();
        var active=AgyAccountCore.GetActiveAccount();
        var stats=AgyAccountCore.GetAccountStats(active);
        var quota=AgyAccountCore.CalculateRollingQuotas(active);
        var grid=new Grid();
        grid.AddColumn(new GridColumn().PadLeft(4));
        grid.AddRow($"[cyan]Active account[/] : [green bold]{active.EscapeMarkup()}[/]");
        grid.AddRow($"[cyan]Login status[/] : {(stats.TokenStatus == "Logged In" ? "[green]● Logged In[/]" : "[red]○ Not Logged In[/]")}");
        grid.AddRow($"[cyan]Weekly quota[/] : {AgyAccountCore.GetProgressBar(quota.RemainingWeekly).EscapeMarkup()}");
        AnsiConsole.Write(grid);
        AnsiConsole.WriteLine();

        try
        {
            var w=WordOfDay.Pick();
            if (w!=null)WordOfDay.Render(w);
        }
        catch
        {
        }
        try
        {
            StudyStreak.ShowPanel();
        }
        catch
        {
        }
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim] Press Enter to continue[/]");
        Console.ReadKey(true);
        AnsiConsole.Clear();

    }

}
public static class CcBanner
{
    public static void Print()
    {
        var acc=AgyAccountCore.GetActiveAccount();
        var displayAcc = acc;
        if (string.Equals(acc, "default", StringComparison.OrdinalIgnoreCase))
        {
            var email = AgyAccountCore.GetAccountEmail("default");
            if (!string.IsNullOrEmpty(email)) displayAcc = $"default ({email})";
        }
        var now=DateTime.Now;
        var w=Math.Min(65, Math.Max(50, Console.WindowWidth-2));
        var sep=new string('=', w);
        AnsiConsole.Clear();
        AnsiConsole.MarkupLine($"[cyan]{sep.EscapeMarkup()}[/]");
        AnsiConsole.MarkupLine("[cyan] ▄████▄ ▄████▄[/] [bold green]🛸 Powershell Profile Control Center v3.0 🛸[/]");
        AnsiConsole.MarkupLine("[cyan] █▀ ▀ █▀ ▀[/] [dim]System dashboard and control suite.[/]");
        AnsiConsole.MarkupLine("[cyan] █ █[/]");
        AnsiConsole.MarkupLine($"[cyan] █▄ ▄ █▄ ▄[/] [dim]Active Account:[/] [green bold]{displayAcc.EscapeMarkup()}[/]");
        AnsiConsole.MarkupLine($"[cyan] ▀████▀ ▀████▀[/] [dim]Time:[/] [yellow]{now:yyyy-MM-dd HH:mm}[/]");
        AnsiConsole.MarkupLine($"[cyan]{sep.EscapeMarkup()}[/]");
        AnsiConsole.MarkupLine("[dim] [[Tab/→]] Navigate Panes | [[←/Esc]] Go Back | [[Enter]] Select & Run[/]");
        AnsiConsole.MarkupLine($"[cyan]{sep.EscapeMarkup()}[/]");
        AnsiConsole.WriteLine();

    }

}
public static class CcNavigator
{
    private sealed record Section(string Label, (string Display, string Alias)[]Items, string Desc);

    private static readonly Section[] AllSections = BuildAllSections();

    private static Section[] BuildAllSections()
    {
        var sections = AgyTui.Registry.CommandRegistry.All
            .GroupBy(c => c.Category)
            .Select(g => new Section(
                g.Key,
                g.Select(c => (c.DisplayName, c.Alias)).ToArray(),
                GetCategoryDescription(g.Key)
            ))
            .ToList();

        sections.Add(new Section("────────────────────────────", Array.Empty<(string, string)>(), ""));
        sections.Add(new Section("[Exit] Exit Control Center", Array.Empty<(string, string)>(), "Exit the Powershell Profile Control Center."));

        return sections.ToArray();
    }

    private static string GetCategoryDescription(string category)
    {
        return category switch
        {
            "[Workspace & Dev]" => "Project navigation, terminal IDE, build/test tools, and git.",
            "[AI Agent & Ollama]" => "Launch AI coding agents, local/cloud models, Antigravity Deck, and agy CLI.",
            "[AGY Account Switch]" => "AGY account switch, tree, rolling quotas, and live status.",
            "[Docker & Databases]" => "Manage docker containers, LocalStack cloud sandbox, and SQLite.",
            "[System & Network]" => "System health, external IP, SSH sessions, and port mapping.",
            "[Learn & Study]" => "Coding quiz, flashcards, language drills, algorithms, and interview prep.",
            "[Track & Progress]" => "Streak tracker, daily goals, Pomodoro focus session, and progress stats.",
            "[Obsidian & Resources]" => "Obsidian markdown vault integration, wikilink graph, and registries.",
            "[Theme & Settings]" => "Profile themes, Command Palette helper, and browser documentation.",
            _ => ""
        };
    }

    private static Section[] GetActiveSections()
    {
        var enableAi = AgyAiCore.IsAiOllamaEnabled();
        var enableAgy = AgyAiCore.IsAgyEnabled();

        var list = new List<Section>();
        foreach (var s in AllSections)
        {
            if (s.Label.Contains("AI Agent & Ollama"))
            {
                if (!enableAi) continue;
                if (!enableAgy)
                {
                    var newItems = s.Items.Select(item => 
                        item.Alias == "agy-cli" ? ("Launch Claude Code CLI (claude)", "agy-cli") : item
                    ).ToArray();
                    list.Add(new Section(s.Label, newItems, s.Desc));
                    continue;
                }
            }
            if (s.Label.Contains("AGY Account Switch") && !enableAgy) continue;

            list.Add(s);
        }
        return list.ToArray();
    }

    public static void Run()
    {
        try
        {
            LearnDataPaths.EnsureDirectories();
            var leftSel=0;
            var midSel=0;
            var midActive=false;
            var detailsActive=false;
            var detailsMode="";
            var detailsSel=0;
            try { Console.CursorVisible=false; } catch {}
            while (true)
            {
                var sections = GetActiveSections();
                if (leftSel >= sections.Length) leftSel = Math.Max(0, sections.Length - 1);

                CcBanner.Print();
                RenderPanes(sections, leftSel, midSel, midActive, detailsActive, detailsSel, detailsMode);
                var key=Console.ReadKey(true);
                var section=sections[leftSel];

                if (detailsActive)
                {
                    int itemsCount = 0;
                    if (detailsMode == "agyswitch")
                    {
                        itemsCount = AgyAccountCore.GetAccounts().Length;
                    }
                    else if (detailsMode == "theme")
                    {
                        itemsCount = GetThemeNames().Length;
                    }
                    else if (detailsMode == "learn" || detailsMode == "session" || detailsMode == "weak")
                    {
                        itemsCount = 6;
                    }
                    else if (detailsMode == "proj")
                    {
                        itemsCount = WorkspaceRegistry.GetWorkspaces().Length;
                    }

                    if (itemsCount == 0)
                    {
                        detailsActive = false;
                        continue;
                    }

                    switch (key.Key)
                    {
                        case ConsoleKey.UpArrow:
                        case ConsoleKey.K:
                            detailsSel = (detailsSel - 1 + itemsCount) % itemsCount;
                            break;
                        case ConsoleKey.DownArrow:
                        case ConsoleKey.J:
                            detailsSel = (detailsSel + 1) % itemsCount;
                            break;
                        case ConsoleKey.Enter:
                            if (detailsSel >= 0 && detailsSel < itemsCount)
                            {
                                if (detailsMode == "agyswitch")
                                {
                                    var accs = AgyAccountCore.GetAccounts();
                                    var targetAcc = accs[detailsSel];
                                    Console.CursorVisible = true;
                                    AgyAccountCore.SetActiveAccount(targetAcc, false);
                                    Console.CursorVisible = false;
                                }
                                else if (detailsMode == "theme")
                                {
                                    var themeNames = GetThemeNames();
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
                                    catch {}
                                    Environment.SetEnvironmentVariable("THEME", selectedTheme);
                                    var themePath = Path.Combine(themesPath, $"{selectedTheme}.omp.json");
                                    var selectedThemeFile = Path.Combine(AgyAccountCore.AgySourceHome, "selected_theme.txt");
                                    File.WriteAllText(selectedThemeFile, themePath);
                                }
                                else if (detailsMode == "learn" || detailsMode == "session" || detailsMode == "weak")
                                {
                                    var topics = new[] { "jp", "en", "cs", "dsa", "interview", "[Type Custom Topic...]" };
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
                                        if (detailsMode == "learn") LearnRouter.StartLearning(selectedTopic);
                                        else if (detailsMode == "session") StudySession.Run(selectedTopic);
                                        else if (detailsMode == "weak") WeakItemsQueue.ShowPreSessionReview(selectedTopic);
                                        Console.CursorVisible = false;
                                    }
                                }
                                else if (detailsMode == "proj")
                                {
                                    var workspaces = WorkspaceRegistry.GetWorkspaces();
                                    var selectedProj = workspaces[detailsSel].WorkspacePath;
                                    var selectedProjFile = Path.Combine(AgyAccountCore.AgySourceHome, "selected_project.txt");
                                    File.WriteAllText(selectedProjFile, selectedProj);
                                    AnsiConsole.MarkupLine($"[green][[Workspace]] Selected workspace '{workspaces[detailsSel].Name}'. Switch will apply on exit.[/]");
                                    Thread.Sleep(1000);
                                }
                                detailsActive = false;
                            }
                            break;
                        case ConsoleKey.A:
                            if (detailsMode == "agyswitch")
                            {
                                var accs = AgyAccountCore.GetAccounts();
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
                                detailsSel = 0;
                            }
                            break;
                        case ConsoleKey.D:
                            if (detailsMode == "agyswitch")
                            {
                                var accs = AgyAccountCore.GetAccounts();
                                if (detailsSel >= 0 && detailsSel < accs.Length)
                                {
                                    var targetAcc = accs[detailsSel];
                                    var activeAcc = AgyAccountCore.GetActiveAccount();
                                    if (string.Equals(targetAcc, activeAcc, StringComparison.OrdinalIgnoreCase))
                                    {
                                        Console.CursorVisible = true;
                                        SpectrePanel.Error($"Cannot delete '{targetAcc}' because it is the current active account.");
                                        Thread.Sleep(1500);
                                        Console.CursorVisible = false;
                                        break;
                                    }
                                    Console.CursorVisible = true;
                                    AnsiConsole.Clear();
                                    var confirm = AnsiConsole.Confirm($"Are you sure you want to delete account '{targetAcc}'?");
                                    if (confirm)
                                    {
                                        try
                                        {
                                            AgyAccountCore.DeleteAccount(targetAcc);
                                            SpectrePanel.Success($"Account '{targetAcc}' deleted successfully!");
                                            Thread.Sleep(1500);
                                        }
                                        catch (Exception ex)
                                        {
                                            SpectrePanel.Error($"Failed to delete account: {ex.Message}");
                                            Thread.Sleep(2000);
                                        }
                                    }
                                    Console.CursorVisible = false;
                                    detailsSel = 0;
                                }
                            }
                            break;
                        case ConsoleKey.O:
                            if (detailsMode == "agyswitch")
                            {
                                var accs = AgyAccountCore.GetAccounts();
                                if (detailsSel >= 0 && detailsSel < accs.Length)
                                {
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
                            }
                            break;
                        case ConsoleKey.LeftArrow:
                        case ConsoleKey.Escape:
                        case ConsoleKey.Q:
                            detailsActive = false;
                            break;
                    }
                    continue;
                }

                if (!midActive)
                {
                    switch (key.Key)
                    {
                        case ConsoleKey.UpArrow:
                        {
                            var next=leftSel;

                            do
                            {
                                next=Math.Max(0, next-1);
                            }
                            while (next>0&&IsSep(sections, next));
                            if (!IsSep(sections, next))
                            {
                                leftSel=next;
                                midSel=0;
                            }
                            break;
                        }
                        case ConsoleKey.DownArrow:
                        {
                            var next=leftSel;

                            do
                            {
                                next=Math.Min(sections.Length-1, next+1);
                            }
                            while (next<sections.Length-1&&IsSep(sections, next));
                            if (!IsSep(sections, next))
                            {
                                leftSel=next;
                                midSel=0;
                            }
                            break;
                        }
                        case ConsoleKey.Enter:case ConsoleKey.RightArrow:case ConsoleKey.Tab:if (section.Label.Contains("Exit"))return;
                        if (section.Items.Length>0)midActive=true;
                        break;
                        case ConsoleKey.Escape:case ConsoleKey.Q when key.Modifiers==0:return;
                    }
                }
                else
                {
                    switch (key.Key)
                    {
                        case ConsoleKey.UpArrow:midSel=Math.Max(0, midSel-1);
                        break;
                        case ConsoleKey.DownArrow:midSel=Math.Min(section.Items.Length-1, midSel+1);
                        break;
                        case ConsoleKey.Enter:
                        case ConsoleKey.RightArrow:
                        case ConsoleKey.Tab:
                        if (midSel<section.Items.Length)
                        {
                            var alias = section.Items[midSel].Alias;
                            if (string.Equals(alias, "agyswitch", StringComparison.OrdinalIgnoreCase))
                            {
                                detailsActive = true;
                                detailsMode = "agyswitch";
                                var accs = AgyAccountCore.GetAccounts();
                                var activeAcc = AgyAccountCore.GetActiveAccount();
                                detailsSel = Array.IndexOf(accs, activeAcc);
                                if (detailsSel < 0) detailsSel = 0;
                            }
                            else if (string.Equals(alias, "theme", StringComparison.OrdinalIgnoreCase))
                            {
                                detailsActive = true;
                                detailsMode = "theme";
                                var themeFiles = GetThemeNames();
                                var currentTheme = Environment.GetEnvironmentVariable("THEME");
                                detailsSel = Array.IndexOf(themeFiles, currentTheme);
                                if (detailsSel < 0) detailsSel = 0;
                            }
                            else if (string.Equals(alias, "learn", StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(alias, "session", StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(alias, "weak", StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(alias, "proj", StringComparison.OrdinalIgnoreCase))
                            {
                                detailsActive = true;
                                detailsMode = alias.ToLowerInvariant();
                                detailsSel = 0;
                            }
                            else if (string.Equals(alias, "account-tree", StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(alias, "quota-chart", StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(alias, "live-dashboard", StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(alias, "disk", StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(alias, "public-ip", StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(alias, "ssh-info", StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(alias, "proj", StringComparison.OrdinalIgnoreCase))
                            {
                                // Rendered directly in details, no execution needed on Enter/Right/Tab
                            }
                            else
                            {
                                Console.CursorVisible=true;
                                Program.RunCommand(alias);
                                AnsiConsole.WriteLine();
                                AnsiConsole.MarkupLine("[dim]Press any key to return to Control Center...[/]");
                                Console.ReadKey(true);
                                Console.CursorVisible=false;
                            }
                        }
                        break;
                        case ConsoleKey.LeftArrow:case ConsoleKey.Escape:midActive=false;
                        break;
                        case ConsoleKey.Q when key.Modifiers==0:return;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            try
            {
                File.WriteAllText(@"C:\Users\TruongNhon\Documents\Powershell\tui_error.txt", ex.ToString());
            }
            catch {}
            throw;
        }
        finally
        {
            Console.CursorVisible=true;
        }

    }

    private static bool IsSep(Section[] sections, int idx) => sections[idx].Label.Length>0&&sections[idx].Label[0]=='─';

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

    private static Spectre.Console.Rendering.IRenderable GetDiskSpaceWidget()
    {
        var table = new Table().Border(TableBorder.Rounded).BorderColor(Color.Cyan1);
        table.AddColumn("[bold cyan]Drive[/]");
        table.AddColumn("[bold cyan]Type[/]");
        table.AddColumn("[bold cyan]TotalSize[/]");
        table.AddColumn("[bold cyan]FreeSpace[/]");
        table.AddColumn("[bold cyan]Used%[/]");
        table.AddColumn("[bold cyan]Health[/]");

        foreach (var d in DriveInfo.GetDrives().Where(d => d.IsReady))
        {
            var usedPct = d.TotalSize > 0 ? Math.Round((1.0 - (double)d.AvailableFreeSpace / d.TotalSize) * 100.0, 1) : 0.0;
            var health = usedPct >= 90 ? "[red]Critical[/]" : usedPct >= 75 ? "[yellow]Warning[/]" : "[green]Healthy[/]";

            static string Fmt(long b) => b > 1_073_741_824 ? $"{Math.Round(b / 1_073_741_824.0, 2)} GB" : $"{Math.Round(b / 1_048_576.0, 2)} MB";
            table.AddRow(d.Name.EscapeMarkup(), d.DriveType.ToString().EscapeMarkup(), Fmt(d.TotalSize), Fmt(d.AvailableFreeSpace), $"{usedPct}%", health);
        }
        return table;
    }

    private static string _cachedIp = null;
    private static DateTime _lastIpFetch = DateTime.MinValue;
    private static Spectre.Console.Rendering.IRenderable GetPublicIpWidget()
    {
        if (_cachedIp == null || (DateTime.Now - _lastIpFetch).TotalMinutes > 5)
        {
            _cachedIp = "Fetching...";
            Task.Run(() => {
                try {
                    _cachedIp = SystemHelper.GetPublicIP();
                } catch {
                    _cachedIp = "Error fetching IP";
                }
                _lastIpFetch = DateTime.Now;
            });
        }
        return new Panel(new Markup($"\n[bold cyan]Public IP Address:[/] [green]{_cachedIp.EscapeMarkup()}[/]\n\n[dim](Refreshes every 5 mins)[/]"))
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Cyan1)
        };
    }

    private static Spectre.Console.Rendering.IRenderable GetSshInfoWidget()
    {
        var localIp = "127.0.0.1";
        try
        {
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    localIp = ip.ToString();
                    break;
                }
            }
        }
        catch {}

        var user = Environment.UserName;
        var hostName = Environment.MachineName;

        var rightSb = new StringBuilder();
        rightSb.AppendLine("[bold white]SSH Connection Info[/]");
        rightSb.AppendLine();
        rightSb.AppendLine("[dim]Local Network Address:[/]");
        rightSb.AppendLine($"  [yellow]ssh {user.ToLowerInvariant()}@{localIp}[/]");
        rightSb.AppendLine();
        rightSb.AppendLine("[dim]Bonjour Hostname Address:[/]");
        rightSb.AppendLine($"  [yellow]ssh {user.ToLowerInvariant()}@{hostName.ToLowerInvariant()}.local[/]");
        rightSb.AppendLine();
        rightSb.AppendLine("[dim]Ensure the Windows SSH service is running.[/]");
        return new Panel(new Markup(rightSb.ToString()))
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Cyan1)
        };
    }

    private static Spectre.Console.Rendering.IRenderable GetAccountTreeWidget()
    {
        var accounts=AgyAccountCore.GetAccounts();
        var active=AgyAccountCore.GetActiveAccount();
        var tree=new Tree("[bold cyan]Account Tree[/]");
        foreach (var acc in accounts)
        {
            var stats=AgyAccountCore.GetAccountStats(acc);
            var displayName = acc;
            if (string.Equals(acc, "default", StringComparison.OrdinalIgnoreCase))
            {
                var email = AgyAccountCore.GetAccountEmail("default");
                if (!string.IsNullOrEmpty(email)) displayName = $"default ({email})";
            }
            var label=acc==active?$"[green bold]★ {displayName.EscapeMarkup()} (Active)[/]":displayName.EscapeMarkup();
            var node=tree.AddNode(label);
            node.AddNode($"[dim]Login:[/] {(stats.TokenStatus == "Logged In" ? "[green]Logged In[/]" : "[red]Not Logged In[/]")}");
            node.AddNode($"[dim]Convos:[/] {stats.ConversationsCount} [dim]Skills:[/] {stats.SkillsCount}");
            node.AddNode($"[dim]Weekly:[/] {(int)Math.Round(stats.GeminiWeekly)}% [dim]5h:[/] {(int)Math.Round(stats.GeminiFiveHour)}%");
            node.AddNode($"[dim]Size:[/] {stats.PrivateSize}");
        }
        return tree;
    }

    private static Spectre.Console.Rendering.IRenderable GetQuotaChartWidget(string accountName)
    {
        var quota=AgyAccountCore.CalculateRollingQuotas(accountName);
        var chartLabel = accountName;
        if (string.Equals(accountName, "default", StringComparison.OrdinalIgnoreCase))
        {
            var email = AgyAccountCore.GetAccountEmail("default");
            if (!string.IsNullOrEmpty(email)) chartLabel = $"default ({email})";
        }
        var chart=new BarChart().Width(28).Label($"[bold cyan]{chartLabel.EscapeMarkup()} Quota Remaining %[/]").CenterLabel()
            .AddItem("Gemini W", quota.RemainingWeekly, Color.Cyan1)
            .AddItem("Gemini 5H", quota.Remaining5H, Color.Yellow)
            .AddItem("Claude W", 100.0, Color.Green)
            .AddItem("Claude 5H", 100.0, Color.Blue);

        var lines = new List<Spectre.Console.Rendering.IRenderable>
        {
            chart,
            new Markup("\n"),
            new Markup($"[dim]Weekly: {quota.CountWeekly,4}/1000 reqs[/]"),
            new Markup($"[dim]5-Hour: {quota.Count5H,4}/50 reqs[/]"),
            new Markup($"[dim]Refreshes in {quota.TimeWeekly}[/]")
        };
        return new Rows(lines);
    }

    private static Spectre.Console.Rendering.IRenderable GetLiveDashboardWidget()
    {
        var table = new Table().Border(TableBorder.Rounded).BorderColor(Color.Grey);
        table.AddColumn("[bold cyan]Account[/]");
        table.AddColumn("[bold cyan]L[/]"); 
        table.AddColumn("[bold cyan]W[/]"); 
        table.AddColumn("[bold cyan]5h[/]"); 
        
        foreach (var a in AgyAccountCore.GetAccounts())
        {
            var s=AgyAccountCore.GetAccountStats(a);
            var act=AgyAccountCore.GetActiveAccount();
            var displayName = a;
            if (string.Equals(a, "default", StringComparison.OrdinalIgnoreCase))
            {
                var email = AgyAccountCore.GetAccountEmail("default");
                if (!string.IsNullOrEmpty(email)) displayName = $"default ({email})";
            }
            var n=a==act?$"[green bold]* {displayName.EscapeMarkup()}[/]":displayName.EscapeMarkup();
            var st=s.TokenStatus=="Logged In"?"[green]●[/]":"[red]○[/]";
            table.AddRow(n, st, $"{(int)Math.Round(s.GeminiWeekly)}%", $"{(int)Math.Round(s.GeminiFiveHour)}%");
        }
        return table;
    }

    public static Table? _cachedOllamaWidget = null;
    public static DateTime _ollamaWidgetCachedAt = DateTime.MinValue;

    private static Spectre.Console.Rendering.IRenderable GetOllamaStatusWidget()
    {
        if (_cachedOllamaWidget != null && (DateTime.UtcNow - _ollamaWidgetCachedAt).TotalSeconds < 3)
        {
            return _cachedOllamaWidget;
        }

        var isRunning = AgyAiCore.IsOllamaRunning();
        var table = new Table().Border(TableBorder.Rounded).BorderColor(isRunning ? Color.Green : Color.Red);
        table.AddColumn("[bold cyan]Ollama Daemon[/]");
        table.AddColumn("[bold cyan]Value[/]");
        
        table.AddRow("Status", isRunning ? "[green bold]● Active (Running)[/]" : "[red bold]○ Offline (Stopped)[/]");
        table.AddRow("Port", "11434");
        
        if (isRunning)
        {
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(1) };
                var response = client.GetStringAsync("http://127.0.0.1:11434/api/tags").Result;
                using var doc = JsonDocument.Parse(response);
                if (doc.RootElement.TryGetProperty("models", out var modelsProp) && modelsProp.ValueKind == JsonValueKind.Array)
                {
                    var modelNames = new List<string>();
                    foreach (var model in modelsProp.EnumerateArray())
                    {
                        if (model.TryGetProperty("name", out var nameProp))
                        {
                            modelNames.Add(nameProp.GetString() ?? "");
                        }
                    }
                    if (modelNames.Count > 0)
                    {
                        table.AddRow("Local Models", string.Join(", ", modelNames));
                    }
                    else
                    {
                        table.AddRow("Local Models", "[yellow]None pulled yet[/]");
                    }
                }
            }
            catch
            {
                table.AddRow("Local Models", "[red]Error listing models[/]");
            }
        }
        _cachedOllamaWidget = table;
        _ollamaWidgetCachedAt = DateTime.UtcNow;
        return table;
    }

    private static void RenderPanes(Section[] sections, int leftSel, int midSel, bool midActive, bool detailsActive, int detailsSel, string detailsMode)
    {
        var leftSb=new StringBuilder();
        for (var i=0;
        i<sections.Length;
        i++)
        {
            var s=sections[i];
            if (s.Label.Length>0&&s.Label[0]=='─')
            {
                leftSb.AppendLine("[dim]────────────────────────────[/]");
                continue;
            }
            if (i==leftSel)leftSb.AppendLine(midActive?$"[cyan bold]> {s.Label.EscapeMarkup()}[/]":$"[green bold]> {s.Label.EscapeMarkup()}[/]");

            else leftSb.AppendLine($" {s.Label.EscapeMarkup()}");
        }
        var section=sections[leftSel];
        var midSb=new StringBuilder();
        for (var i=0;
        i<section.Items.Length;
        i++)
        {
            var(display, _)=section.Items[i];
            midSb.AppendLine(midActive&&i==midSel?$"[green bold]> {display.EscapeMarkup()}[/]":$" {display.EscapeMarkup()}");
        }
        if (section.Items.Length==0)midSb.AppendLine("[dim] (press Enter to select)[/]");
        
        var sectionTitle=section.Label.TrimStart('>',' ');
        Spectre.Console.Rendering.IRenderable detailsContent;

        if (midActive&&midSel<section.Items.Length)
        {
            var(display, alias)=section.Items[midSel];
            
            if (string.Equals(alias, "account-tree", StringComparison.OrdinalIgnoreCase))
            {
                detailsContent = GetAccountTreeWidget();
            }
            else if (string.Equals(alias, "quota-chart", StringComparison.OrdinalIgnoreCase))
            {
                detailsContent = GetQuotaChartWidget(AgyAccountCore.GetActiveAccount());
            }
            else if (string.Equals(alias, "live-dashboard", StringComparison.OrdinalIgnoreCase))
            {
                detailsContent = GetLiveDashboardWidget();
            }
            else if (string.Equals(alias, "disk", StringComparison.OrdinalIgnoreCase))
            {
                detailsContent = GetDiskSpaceWidget();
            }
            else if (string.Equals(alias, "public-ip", StringComparison.OrdinalIgnoreCase))
            {
                detailsContent = GetPublicIpWidget();
            }
            else if (string.Equals(alias, "ssh-info", StringComparison.OrdinalIgnoreCase))
            {
                detailsContent = GetSshInfoWidget();
            }
            else if (string.Equals(alias, "agyswitch", StringComparison.OrdinalIgnoreCase) && detailsActive && detailsMode == "agyswitch")
            {
                var rightSb = new StringBuilder();
                rightSb.AppendLine($"[bold white]{display.EscapeMarkup()}[/]");
                rightSb.AppendLine($"[dim]alias:[/] [yellow]{alias.EscapeMarkup()}[/]");
                rightSb.AppendLine();
                rightSb.AppendLine("[cyan bold]Select Account to Switch:[/]");
                rightSb.AppendLine();
                var accs = AgyAccountCore.GetAccounts();
                var activeAcc = AgyAccountCore.GetActiveAccount();
                for (var i = 0; i < accs.Length; i++)
                {
                    var isSelected = (i == detailsSel);
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
                    rightSb.AppendLine($"{prefix}{displayName.EscapeMarkup()} [dim]({loginStatus})[/]{suffix}");
                }
                rightSb.AppendLine();
                rightSb.AppendLine("[dim]↑/↓ Navigate  ·  Enter Select  ·  Esc Cancel[/]");
                rightSb.AppendLine("[dim]a Create Account  ·  d Delete  ·  o Log Out[/]");
                detailsContent = new Markup(rightSb.ToString());
            }
            else if (string.Equals(alias, "theme", StringComparison.OrdinalIgnoreCase) && detailsActive && detailsMode == "theme")
            {
                var rightSb = new StringBuilder();
                rightSb.AppendLine($"[bold white]{display.EscapeMarkup()}[/]");
                rightSb.AppendLine($"[dim]alias:[/] [yellow]{alias.EscapeMarkup()}[/]");
                rightSb.AppendLine();
                rightSb.AppendLine("[cyan bold]Select Oh My Posh Theme (Color segment preview):[/]");
                rightSb.AppendLine();
                var themeNames = GetThemeNames();
                var currentTheme = Environment.GetEnvironmentVariable("THEME");
                for (var i = 0; i < themeNames.Length; i++)
                {
                    var isSelected = (i == detailsSel);
                    var isActive = (themeNames[i] == currentTheme);
                    var prefix = isSelected ? "[green bold]> [/]" : "  ";
                    var suffix = isActive ? " [green](Active)[/]" : "";
                    rightSb.AppendLine($"{prefix}{themeNames[i].EscapeMarkup()}{suffix}");
                }
                rightSb.AppendLine();
                rightSb.AppendLine("[dim]↑/↓ Navigate  ·  Enter Select  ·  Esc Cancel[/]");
                detailsContent = new Markup(rightSb.ToString());
            }
            else if ((string.Equals(alias, "learn", StringComparison.OrdinalIgnoreCase) ||
                      string.Equals(alias, "session", StringComparison.OrdinalIgnoreCase) ||
                      string.Equals(alias, "weak", StringComparison.OrdinalIgnoreCase)) && detailsActive && detailsMode == alias.ToLowerInvariant())
            {
                var rightSb = new StringBuilder();
                rightSb.AppendLine($"[bold white]{display.EscapeMarkup()}[/]");
                rightSb.AppendLine($"[dim]alias:[/] [yellow]{alias.EscapeMarkup()}[/]");
                rightSb.AppendLine();
                rightSb.AppendLine($"[cyan bold]Select Topic for {alias.EscapeMarkup()}:[/]");
                rightSb.AppendLine();
                var topics = new[] { "jp (Japanese / Language)", "en (English Vocabulary)", "cs (C# Quiz)", "dsa (Data Structures & Algorithms)", "interview (Question Bank & STAR)", "[Type Custom Topic...]" };
                for (var i = 0; i < topics.Length; i++)
                {
                    var isSelected = (i == detailsSel);
                    var prefix = isSelected ? "[green bold]> [/]" : "  ";
                    rightSb.AppendLine($"{prefix}{topics[i].EscapeMarkup()}");
                }
                rightSb.AppendLine();
                rightSb.AppendLine("[dim]↑/↓ Navigate  ·  Enter Select  ·  Esc Cancel[/]");
                detailsContent = new Markup(rightSb.ToString());
            }
            else if (string.Equals(alias, "proj", StringComparison.OrdinalIgnoreCase) && detailsActive && detailsMode == "proj")
            {
                var rightSb = new StringBuilder();
                rightSb.AppendLine($"[bold white]{display.EscapeMarkup()}[/]");
                rightSb.AppendLine($"[dim]alias:[/] [yellow]{alias.EscapeMarkup()}[/]");
                rightSb.AppendLine();
                rightSb.AppendLine("[cyan bold]Select Workspace directory to switch to on exit:[/]");
                rightSb.AppendLine();
                var workspaces = WorkspaceRegistry.GetWorkspaces();
                for (var i = 0; i < workspaces.Length; i++)
                {
                    var isSelected = (i == detailsSel);
                    var prefix = isSelected ? "[green bold]> [/]" : "  ";
                    rightSb.AppendLine($"{prefix}{workspaces[i].Name.EscapeMarkup()} [dim]({workspaces[i].WorkspacePath.EscapeMarkup()})[/]");
                }
                rightSb.AppendLine();
                rightSb.AppendLine("[dim]↑/↓ Navigate  ·  Enter Select  ·  Esc Cancel[/]");
                detailsContent = new Markup(rightSb.ToString());
            }
            else
            {
                var rightSb = new StringBuilder();
                rightSb.AppendLine($"[bold white]{display.EscapeMarkup()}[/]");
                rightSb.AppendLine($"[dim]alias:[/] [yellow]{alias.EscapeMarkup()}[/]");
                
                var cmd=CommandPalette.Commands.FirstOrDefault(c => string.Equals(c.Alias, alias, StringComparison.OrdinalIgnoreCase));
                if (cmd!=null)
                {
                    rightSb.AppendLine();
                    rightSb.AppendLine($"[dim]{cmd.Description.EscapeMarkup()}[/]");
                    rightSb.AppendLine();
                    rightSb.AppendLine($"[dim]Category: {cmd.Category.EscapeMarkup()}[/]");
                }
                if (alias == "ollama-status")
                {
                    detailsContent = new Rows(new Markup(rightSb.ToString()), new Markup("\n"), GetOllamaStatusWidget());
                }
                else
                {
                    detailsContent = new Markup(rightSb.ToString());
                }
            }
        }
        else
        {
            var rightSb = new StringBuilder();
            rightSb.AppendLine($"[bold cyan]{sectionTitle.EscapeMarkup()}[/]");
            rightSb.AppendLine();
            if (!string.IsNullOrWhiteSpace(section.Desc))rightSb.AppendLine($"[dim]{section.Desc.EscapeMarkup()}[/]");
            rightSb.AppendLine();
            rightSb.AppendLine("[dim]Press → or Enter to browse options[/]");
            detailsContent = new Markup(rightSb.ToString());
        }

        var leftPanel=new Panel(leftSb.ToString())
        {
            Header=new PanelHeader("[bold cyan]Menu[/]"), Border=BoxBorder.Rounded, BorderStyle=new Style(!midActive?Color.Cyan1:Color.Grey)
        };
        var midPanel=new Panel(midSb.ToString())
        {
            Header=new PanelHeader("[bold cyan]Options[/]"), Border=BoxBorder.Rounded, BorderStyle=new Style(midActive && !detailsActive?Color.Cyan1:Color.Grey)
        };
        var rightPanel=new Panel(detailsContent)
        {
            Header=new PanelHeader("[bold cyan]Details[/]"), Border=BoxBorder.Rounded, BorderStyle=new Style(detailsActive?Color.Cyan1:Color.Grey)
        };

        var table = new Table().NoBorder().HideHeaders();
        table.AddColumn(new TableColumn("").Width(30));
        table.AddColumn(new TableColumn("").Width(30));
        table.AddColumn(new TableColumn(""));
        table.AddRow(leftPanel, midPanel, rightPanel);
        AnsiConsole.Write(table);

    }

}
public static class Program
{
    public static void Main(string[]args)
    {
        try
        {
            AgyTui.Registry.CommandRegistry.AssertSwitchCases();
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            Environment.Exit(1);
        }

        if (args.Length>0)
        {
            RunCommand(args[0]);
            return;
        }
        CcNavigator.Run();

        try
        {
            AnsiConsole.Clear();
        }
        catch
        {
        }
        AnsiConsole.MarkupLine("[dim]Goodbye.[/]");

    }

    public static string? SelectTopicInteractive(string promptTitle)
    {
        var topics = new[] { "jp (Japanese / Language)", "en (English Vocabulary)", "cs (C# Quiz)", "dsa (Data Structures & Algorithms)", "interview (Question Bank & STAR)", "[Type Custom Topic...]" };
        var index = SpectreMenu.ShowWithEscape(promptTitle, topics, 0);
        if (index < 0) return null;
        if (index == topics.Length - 1)
        {
            Console.CursorVisible = true;
            var custom = AnsiConsole.Ask<string>("Enter custom topic name:").Trim();
            Console.CursorVisible = false;
            return string.IsNullOrEmpty(custom) ? null : custom;
        }
        return topics[index].Split(' ')[0];
    }

    public static void RunCommand(string alias)
    {
        try
        {
            AnsiConsole.Clear();
        }
        catch
        {
        }
        var lAlias = alias.ToLowerInvariant();
        if ((lAlias == "claude" || lAlias == "codex" || lAlias == "openclaw" || lAlias == "hermes" || lAlias == "hermesd" || lAlias == "claude-cloud" || lAlias == "claude-ollama" || lAlias == "codex-cloud" || lAlias == "codex-ollama") && !AgyAiCore.IsAiOllamaEnabled())
        {
            SpectrePanel.Error("AI/Ollama features are disabled in config.");
            Thread.Sleep(1500);
            return;
        }
        if ((lAlias == "agyswitch" || lAlias == "agyquota" || lAlias == "account-tree" || lAlias == "quota-chart" || lAlias == "live-dashboard" || lAlias == "autoswitch") && !AgyAiCore.IsAgyEnabled())
        {
            SpectrePanel.Error("AGY Account features are disabled in config.");
            Thread.Sleep(1500);
            return;
        }

        try
        {
            switch (alias.ToLowerInvariant())
            {
                case"proj":case"prj":var projPath=ProfileNavigator.Navigate("");
                if (!string.IsNullOrEmpty(projPath))
                {
                    AnsiConsole.MarkupLine($"Navigate target: [green]{projPath}[/]");
                }
                break;
                case"gs":GitHelper.ShowStatus();
                break;
                case"gcmt":GitHelper.ConventionalCommitWizard();
                break;
                case"git-undo":GitHelper.InvokeGitUndo();
                break;
                case"dbld":DotNetHelper.Build();
                break;
                case"dtst":DotNetHelper.Test();
                break;
                case"clean-build":DotNetHelper.RemoveBinObj(Directory.GetCurrentDirectory());
                break;
                case"add-migration":var migName=AnsiConsole.Ask<string>("Migration name:");
                DotNetHelper.AddMigration(migName);
                break;
                case"update-db":DotNetHelper.UpdateDatabase();
                break;
                case"dkcl":DockerHelper.ShowCleanupDashboard();
                break;
                case"dcup":DockerHelper.ComposeUp();
                break;
                case"dcdown":DockerHelper.ComposeDown();
                break;
                case"aws-local":AwsHelper.ShowLocalStackInfo();
                break;
                case"claude":AgyAiCore.InvokeClaude([]);
                break;
                case"claude-cloud":AgyAiCore.InvokeClaude([], "cloud");
                break;
                case"claude-ollama":AgyAiCore.InvokeClaude([], "local");
                break;
                case"codex":AgyAiCore.InvokeCodex([]);
                break;
                case"codex-cloud":AgyAiCore.InvokeCodex([], "cloud");
                break;
                case"codex-ollama":AgyAiCore.InvokeCodex([], "local");
                break;
                case"openclaw":AgyAiCore.InvokeOpenClaw([]);
                 break;
                 case"ollama-models":OllamaHelper.ManageOllamaModels();
                 break;
                 case"ollama-pull":OllamaHelper.PullOllamaModel();
                 break;
                 case"ollama-start":OllamaHelper.StartOllamaDaemon();
                 break;
                 case"ollama-logs":OllamaHelper.ShowOllamaLogs();
                break;
                case"ollama-status":
                    CcNavigator._cachedOllamaWidget = null;
                    break;
                case"deck-setup":AntigravityDeckHelper.Setup();
                break;
                case"deck-start":AntigravityDeckHelper.StartLocal();
                break;
                case"deck-online":AntigravityDeckHelper.StartOnline();
                break;
                case"agy-cli":
                    if (!AgyAiCore.IsAgyEnabled())
                    {
                        AgyAiCore.InvokeClaude([]);
                        break;
                    }
                    try
                    {
                        var targetDirLoc = AgyAccountCore.GetAccountDirectory(AgyAccountCore.GetActiveAccount());
                        var psi = new ProcessStartInfo
                        {
                            FileName = "cmd.exe",
                            Arguments = "/c agy",
                            UseShellExecute = false
                        };
                        psi.EnvironmentVariables["GEMINI_HOME"] = targetDirLoc;
                        psi.EnvironmentVariables["HOME"] = targetDirLoc;
                        psi.EnvironmentVariables["USERPROFILE"] = targetDirLoc;

                        using var p = Process.Start(psi);
                        p?.WaitForExit();
                    }
                    catch (Exception ex)
                    {
                        SpectrePanel.Error($"Failed to run agy CLI: {ex.Message}");
                    }
                    break;
                case"hermes":if (AgyAiCore.InvokeHermes([])==AgyAiCore.HermesResult.NotInstalled)SpectrePanel.Warning("Hermes is not installed. Run 'hermes' from PowerShell for install instructions.");
                break;
                case"hermesd":if (AgyAiCore.InvokeHermesDesktop([])==AgyAiCore.HermesResult.NotInstalled)SpectrePanel.Warning("Hermes Desktop is not installed. Run 'hermesd' from PowerShell for install instructions.");
                break;
                case"disk":SystemHelper.ShowDiskSpace();
                break;
                case"public-ip":AnsiConsole.MarkupLine($"Public IP: [green]{SystemHelper.GetPublicIP()}[/]");
                break;
                case"kill-port":var portStr=AnsiConsole.Ask<string>("Port number:");
                if (int.TryParse(portStr, out var port))SystemHelper.KillPort(port);
                break;
                case"ssh-info":SshHelper.ShowSshInfo();
                break;
                case"db-tui":var dbPath=AnsiConsole.Ask<string>("SQLite DB path:");
                DatabaseHelper.ShowDatabaseTui(dbPath);
                break;
                case"agyswitch":var accs=AgyAccountCore.GetAccounts();
                var activeAcc=AgyAccountCore.GetActiveAccount();
                var accItems=accs.Select(a => a==activeAcc?$"{a} (Active)":a).ToArray();
                var defaultIdx=Array.IndexOf(accs, activeAcc);
                if (defaultIdx<0)defaultIdx=0;
                var accIdx=SpectreMenu.ShowWithEscape("Select Account to Switch", accItems, defaultIdx);
                if (accIdx>=0)
                {
                    var targetAcc=accs[accIdx];
                    AgyAccountCore.SetActiveAccount(targetAcc, false);
                    Thread.Sleep(1000);
                }
                break;
                case"agyquota":AgyAccountCore.ShowAllAccountsSummary();
                break;
                case"account-tree":AgyAccountDisplay.ShowAccountTree();
                break;
                case"quota-chart":AgyAccountDisplay.ShowQuotaChart(AgyAccountCore.GetActiveAccount());
                break;
                case"live-dashboard":SpectreTable.Live(["Account","Login","Quota W","Quota 5h","Last Used"], () => AgyAccountCore.GetAccounts().Select(a =>
                {
                    var s=AgyAccountCore.GetAccountStats(a);
                    var act=AgyAccountCore.GetActiveAccount();
                    var n=a==act?$"[green bold]* {a}[/]":a;
                    var st=s.TokenStatus=="Logged In"?"[green]●[/]":"[red]○[/]";
                    var lu=s.LastUsed.Length>=10&&s.LastUsed!="Never"?s.LastUsed[..10]:"Never";
                    return new[]
                    {
                        n, st,$"{(int)Math.Round(s.GeminiWeekly)}%",$"{(int)Math.Round(s.GeminiFiveHour)}%", lu
                    }
                    ;
                }
                ).ToArray(), 5000);
                break;
                case"autoswitch":AgyAccountCore.ToggleAutoSwitch();
                break;
                case"scaffold":ProjectScaffolder.Scaffold();
                break;
                case"help":ProfileHelp.Show();
                break;
                case"theme":
                {
                    var tPath = Environment.GetEnvironmentVariable("POSH_THEMES_PATH");
                    if (string.IsNullOrEmpty(tPath))
                    {
                        tPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "asset", "powershell-themes");
                        if (!Directory.Exists(tPath))
                        {
                            tPath = Path.Combine(Directory.GetCurrentDirectory(), "asset", "powershell-themes");
                        }
                    }
                    var currTheme = Environment.GetEnvironmentVariable("THEME");
                    var newThemePath = ThemeHelper.SelectThemeInteractive(tPath, currTheme);
                    if (!string.IsNullOrEmpty(newThemePath))
                    {
                        var selThemeFile = Path.Combine(AgyAccountCore.AgySourceHome, "selected_theme.txt");
                        File.WriteAllText(selThemeFile, newThemePath);
                    }
                }
                break;
                case"cc":CommandPalette.Show();
                break;
                case"learn":var learnTopic=SelectTopicInteractive("Select Topic to Learn");
                if (!string.IsNullOrEmpty(learnTopic)) LearnRouter.StartLearning(learnTopic);
                break;
                case"flashcard":FlashcardEngine.PickAndRun(LearnDataPaths.DecksDir);
                break;
                case"vocab":VocabDrill.Run("Intermediate");
                break;
                case"kana":KanaQuiz.Run("hiragana");
                break;
                case"kanji":KanjiLookup.Run();
                break;
                case"jlpt":JlptVocabDrill.Run("N5");
                break;
                case"algo":AlgoVisualizer.PickAndRun();
                break;
                case"complexity":ComplexitySheet.Run();
                break;
                case"problems":ProblemTracker.Run();
                break;
                case"snippets":SnippetLibrary.Run();
                break;
                case"sheets":CheatSheetBrowser.Run();
                break;
                case"quiz":CsharpQuiz.Run();
                break;
                case"interview":InterviewBank.Run();
                break;
                case"star":StarBuilder.Run();
                break;
                case"mock":MockInterviewTimer.Run();
                break;
                case"word-of-day":var word=WordOfDay.Pick();
                if (word!=null)WordOfDay.Render(word);

                else SpectrePanel.Warning("No word of the day available.");
                break;
                case"session":var sessionTopic=SelectTopicInteractive("Select Topic for Study Session");
                if (!string.IsNullOrEmpty(sessionTopic)) StudySession.Run(sessionTopic);
                break;
                case"stats":StudyStats.Run();
                break;
                case"goals":DailyGoals.Show();
                break;
                case"streak":StudyStreak.ShowPanel();
                break;
                case"due":LearnDataPaths.EnsureDirectories();
                int dueCount=0;
                if (Directory.Exists(LearnDataPaths.DecksDir))
                {
                    foreach (var deckFile in Directory.GetFiles(LearnDataPaths.DecksDir,"*.json"))
                    {
                        var deck=LearnDataPaths.LoadJson<DeckFile>(deckFile);
                        if (deck!=null)
                        {
                            dueCount+=deck.Cards.Count(c => SpacedRepetitionEngine.IsDueToday(c.Sr));
                        }
                    }
                }
                AnsiConsole.MarkupLine($"Due spaced repetition reviews today: [yellow]{dueCount}[/]");
                break;
                case"progress":ProgressDashboard.Show();
                break;
                case"weak":var weakTopic=SelectTopicInteractive("Select Topic for Weak Items");
                if (!string.IsNullOrEmpty(weakTopic)) WeakItemsQueue.ShowPreSessionReview(weakTopic);
                break;
                case"obsidian":ObsidianBridge.Run();
                break;
                case"obs-graph":var cfg=ObsidianBridge.LoadConfig();
                if (cfg!=null&&Directory.Exists(cfg.VaultPath))ObsidianGraph.Run(cfg.VaultPath);

                else SpectrePanel.Warning("Obsidian vault path not configured. Run 'obsidian' first.");
                break;
                case"nexus":case"repo-graph":RepoGraph.Show(RepoGraph.Build());
                break;
                case"nexus-stats":GitNexusStats.Run();
                break;
                case"ide":TerminalIde.Open();
                break;
                case"ide-diff":GitDiffViewer.ShowDiff(Directory.GetCurrentDirectory());
                break;
                case"ide-search":AnsiConsole.MarkupLine("IDE Search: Browse symbols for current directory files.");
                break;
                case"refresh":LearnRouter.RefreshData("all");
                break;
                case"add-resource":var path=AnsiConsole.Ask<string>("Resource path/URL:");
                var tags=AnsiConsole.Ask<string>("Tags (comma separated):").Split(',').Select(t => t.Trim()).ToArray();
                ResourceRegistry.AddResource(path, tags);
                SpectrePanel.Success("Resource registered.");
                break;
                default:SpectrePanel.Warning($"Command alias '{alias}' is not implemented for direct TUI routing.");
                break;
            }
        }
        catch (Exception ex)
        {
            SpectrePanel.Error($"Error running command: {ex.Message}");
        }
        AnsiConsole.WriteLine();
        if (!Console.IsInputRedirected)
        {
            AnsiConsole.MarkupLine("[dim]Press any key to return to menu...[/]");

            try
            {
                Console.ReadKey(true);
            }
            catch
            {
            }
        }

    }

}
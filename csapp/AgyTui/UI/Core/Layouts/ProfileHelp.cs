using System.Collections.Frozen;
using System.Text.Json;

namespace AgyTui.UI.Core.Layouts;

public sealed record CommandDoc(string Alias, string FullName, string Desc, string Command);

public static class ProfileHelp
{
    private static readonly FrozenDictionary<string, string[]> HelpTopics = AgyTui.Core.Registries.CommandRegistry.All
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
        var topics = HelpTopics.Keys.ToArray();
        var idx = SpectreMenu.Show("Help Topics", topics, 0, true);
        if (idx < 0) return;
        var topic = topics[idx];
        SpectrePager.Show($"Help: {topic}", HelpTopics[topic]);
    }

    public static Dictionary<string, Dictionary<string, CommandDoc[]>> GetCommands(string jsonPath)
    {
        if (!File.Exists(jsonPath)) return new();

        try
        {
            var raw = File.ReadAllText(jsonPath);
            var opts = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            return JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, CommandDoc[]>>>(raw, opts) ?? new();
        }
        catch
        {
            return new();
        }
    }

    public static CommandDoc? ShowInteractive(string jsonPath, string initialFilter)
    {
        var cmdsNested = GetCommands(jsonPath);
        var cmds = new Dictionary<string, CommandDoc[]>();
        foreach (var (_, subDict) in cmdsNested) foreach (var (sub, docs) in subDict) cmds[sub] = docs;
        var categories = cmds.Keys.OrderBy(k => k, StringComparer.Ordinal).ToArray();
        var allCommands = categories.SelectMany(c => cmds[c]).ToArray();
        var categoryLookup = new Dictionary<string, string>();
        foreach (var c in categories) categoryLookup[$"{c} ({cmds[c].Length} commands)"] = c;
        var commandLookup = new Dictionary<string, CommandDoc>();
        foreach (var c in allCommands) commandLookup[$"{c.Alias,-10} - {c.Desc}"] = c;
        string[] TopResolver(string filter)
        {
            if (string.IsNullOrWhiteSpace(filter)) return categories.Select(c => $"{c} ({cmds[c].Length} commands)").ToArray();
            return allCommands.Where(c => c.Alias.Contains(filter, StringComparison.OrdinalIgnoreCase) || c.Desc.Contains(filter, StringComparison.OrdinalIgnoreCase) || c.Command.Contains(filter, StringComparison.OrdinalIgnoreCase)).Select(c => $"{c.Alias,-10} - {c.Desc}").ToArray();
        }
        var filter = initialFilter;
        while (true)
        {
            var selectedLabel = SpectreMenu.ShowDynamic("Select Help Category", TopResolver, 0, filter);
            filter = "";
            if (selectedLabel == null) return null;
            if (commandLookup.TryGetValue(selectedLabel, out var cmdObj)) return cmdObj;
            if (categoryLookup.TryGetValue(selectedLabel, out var catName))
            {
                var catCmds = cmds[catName];
                var subLookup = new Dictionary<string, CommandDoc>();
                foreach (var c in catCmds) subLookup[$"{c.Alias,-10} - {c.Desc}"] = c;
                string[] SubResolver(string subFilter) => catCmds.Where(c => string.IsNullOrWhiteSpace(subFilter) || c.Alias.Contains(subFilter, StringComparison.OrdinalIgnoreCase) || c.Desc.Contains(subFilter, StringComparison.OrdinalIgnoreCase) || c.Command.Contains(subFilter, StringComparison.OrdinalIgnoreCase)).Select(c => $"{c.Alias,-10} - {c.Desc}").ToArray();
                while (true)
                {
                    var selectedSubLabel = SpectreMenu.ShowDynamic($"Category: {catName}", SubResolver, 0);
                    if (selectedSubLabel == null) break;
                    if (subLookup.TryGetValue(selectedSubLabel, out var subCmd)) return subCmd;
                }
            }
        }
    }
}

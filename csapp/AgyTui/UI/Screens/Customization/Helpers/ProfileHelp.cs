using System.Collections.Frozen;
using System.Text.Json;
using AgyTui.UI.Core.Components;
using AgyTui.UI.Core.Commands;
using Spectre.Console;

namespace AgyTui.UI.Screens.Customization.Helpers;

public interface IProfileHelp
{
    void Show();
    void ShowTopic(string topic);
}

public sealed record CommandDoc(string Alias, string FullName, string Desc, string Command);

public class ProfileHelpService : IProfileHelp
{
    private static readonly FrozenDictionary<string, string[]> HelpTopics = CommandRegistry.All
        .Where(c => c.HelpLines.Length > 0)
        .GroupBy(c => c.HelpCategory)
        .ToDictionary(
            g => g.Key,
            g => g.SelectMany(c => c.HelpLines).ToArray(),
            StringComparer.OrdinalIgnoreCase
        )
        .ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    public void Show()
    {
        var topics = HelpTopics.Keys.OrderBy(k => k).ToArray();

        var choice = SpectreMenu.ShowDynamic("🛸 Profile Comprehensive Documentation & Help Hub", filter =>
        {
            var matched = topics.Where(t => string.IsNullOrEmpty(filter) || t.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();
            matched.Add("Exit");
            return matched.ToArray();
        }, 0);

        if (string.IsNullOrEmpty(choice) || choice == "Exit") return;

        if (HelpTopics.TryGetValue(choice, out var lines))
        {
            SpectrePager.Show($"Docs: {choice}", lines);
        }
    }

    public void ShowTopic(string topic)
    {
        if (HelpTopics.TryGetValue(topic, out var lines))
        {
            SpectrePager.Show($"Docs: {topic}", lines);
        }
        else
        {
            SpectrePanel.Warning($"Help topic '{topic}' not found.");
        }
    }
}

public static class ProfileHelp
{
    private static readonly IProfileHelp _service = new ProfileHelpService();
    public static IProfileHelp Instance => _service;

    public static void Show() => _service.Show();
    public static void ShowTopic(string topic) => _service.ShowTopic(topic);
}


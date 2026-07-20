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
    private static readonly string ConfigFile = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".gemini", "antigravity", "priority_workspaces.json");

    public static WorkspaceEntry[] GetWorkspaces()
    {
        if (!File.Exists(ConfigFile)) return [];

        try
        {
            var raw = File.ReadAllText(ConfigFile);
            return JsonSerializer.Deserialize<WorkspaceEntry[]>(raw) ?? [];
        }
        catch
        {
            return [];
        }

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
        }
        catch (Exception ex)
        {
            SpectrePanel.Error($"Failed to save workspaces: {ex.Message}");
        }

    }

    public static WorkspaceEntry[] FindByQuery(string query)
    {
        var all = GetWorkspaces();
        if (string.IsNullOrWhiteSpace(query)) return all;

        try
        {
            return all.Where(w => Regex.IsMatch(w.Name, query, RegexOptions.IgnoreCase) || Regex.IsMatch(w.WorkspacePath, query, RegexOptions.IgnoreCase)).ToArray();
        }
        catch
        {
            return [];
        }

    }

    public static WorkspaceEntry[] GetByAccount(string accountName) => GetWorkspaces().Where(w => string.Equals(w.AssociatedAccount, accountName, StringComparison.OrdinalIgnoreCase)).ToArray();

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
        if (matches.Length == 1) return matches[0].WorkspacePath;
        var idx = SpectreMenu.Show("Navigate to Workspace", matches.Select(m => m.Name).ToArray(), 0, true);
        return idx >= 0 ? matches[idx].WorkspacePath : null;

    }

}

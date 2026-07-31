using System.Diagnostics;
using System.Net.Http;
using System.Text.RegularExpressions;
using Spectre.Console;

namespace AgyTui.Infrastructure.Common;

public static class SystemHelper
{
    public static bool IsFuzzyMatch(string text, string pattern)
    {
        if (string.IsNullOrEmpty(pattern)) return true;
        if (string.IsNullOrEmpty(text)) return false;

        text = text.ToLowerInvariant();
        pattern = pattern.ToLowerInvariant();

        if (text.Contains(pattern)) return true;

        int patternIdx = 0;
        for (int textIdx = 0; textIdx < text.Length; textIdx++)
        {
            if (text[textIdx] == pattern[patternIdx])
            {
                patternIdx++;
                if (patternIdx == pattern.Length) return true;
            }
        }
        return false;
    }

    public static string BoldFuzzyMatch(string text, string pattern)
    {
        if (string.IsNullOrEmpty(pattern) || string.IsNullOrEmpty(text))
            return text.EscapeMarkup();

        if (text.Contains(pattern, StringComparison.OrdinalIgnoreCase))
        {
            int idx = text.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
            var before = text[..idx].EscapeMarkup();
            var match = text.Substring(idx, pattern.Length).EscapeMarkup();
            var after = text[(idx + pattern.Length)..].EscapeMarkup();
            return $"{before}[bold yellow]{match}[/]{after}";
        }

        return text.EscapeMarkup();
    }

    public static void OpenExplorer(string path = "")
    {
        if (string.IsNullOrEmpty(path)) path = Directory.GetCurrentDirectory();
        try
        {
            if (Directory.Exists(path) || File.Exists(path))
            {
                Process.Start(new ProcessStartInfo("explorer.exe", $"\"{path}\"") { UseShellExecute = true });
            }
        }
        catch { }
    }

    public static void OpenNewTerminalSession(string path = "", string title = "")
    {
        if (string.IsNullOrEmpty(path)) path = Directory.GetCurrentDirectory();
        try
        {
            var wt = ProcessRunner.FindOnPath("wt.exe") ?? ProcessRunner.FindOnPath("wt");
            if (!string.IsNullOrEmpty(wt))
            {
                Process.Start(new ProcessStartInfo(wt, $"-d \"{path}\"") { UseShellExecute = true });
            }
            else
            {
                Process.Start(new ProcessStartInfo("powershell.exe", $"-NoExit -Command \"Set-Location '{path}'\"") { UseShellExecute = true });
            }
        }
        catch { }
    }

    public static void ShowDiskSpace()
    {
        try
        {
            var drives = DriveInfo.GetDrives().Where(d => d.IsReady);
            foreach (var d in drives)
            {
                var freeGb = d.AvailableFreeSpace / 1073741824.0;
                var totalGb = d.TotalSize / 1073741824.0;
                AnsiConsole.MarkupLine($"[cyan]Drive {d.Name}[/] {freeGb:F1} GB free of {totalGb:F1} GB");
            }
        }
        catch { }
    }

    public static string GetPublicIP()
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            return client.GetStringAsync("https://api.ipify.org").Result.Trim();
        }
        catch
        {
            return "Unavailable";
        }
    }

    public static void KillPort(int port)
    {
        try
        {
            var psi = new ProcessStartInfo("cmd.exe", $"/c netstat -ano | findstr :{port}")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            if (p != null)
            {
                var output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                var matches = Regex.Matches(output, @"\s+(\d+)$");
                foreach (Match m in matches)
                {
                    if (m.Success && int.TryParse(m.Groups[1].Value, out var pid))
                    {
                        Process.GetProcessById(pid).Kill();
                        AnsiConsole.MarkupLine($"[green]Killed process {pid} on port {port}.[/]");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Failed to kill port {port}: {ex.Message}[/]");
        }
    }
}

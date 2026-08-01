using System.Diagnostics;
using System.Net.Http;
using System.Text.RegularExpressions;
using Spectre.Console;

namespace AgyTui.Infrastructure.Common;

public class SystemHelper : ISystemHelper
{
    private static readonly Lazy<SystemHelper> _instance = new(() => new SystemHelper());
    public static SystemHelper Instance => _instance.Value;

    public bool IsFuzzyMatch(string text, string pattern)
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

    public string BoldFuzzyMatch(string text, string pattern)
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

    public void OpenExplorer(string path = "")
    {
        if (string.IsNullOrEmpty(path)) path = Directory.GetCurrentDirectory();
        try
        {
            if (Directory.Exists(path) || File.Exists(path))
            {
                Process.Start(new ProcessStartInfo("explorer.exe", $"\"{path}\"") { UseShellExecute = true });
            }
        }
        catch (Exception ex)
        {
            LogHelper.LogError("OpenExplorer failed", ex);
        }
    }

    public void OpenNewTerminalSession(string path = "", string? initialCommand = null, bool promptOptions = false)
    {
        if (string.IsNullOrEmpty(path)) path = Directory.GetCurrentDirectory();

        if (promptOptions && string.IsNullOrEmpty(initialCommand))
        {
            var options = new[]
            {
                "⚡ Blank Shell (No command)",
                "🔨 Build Project (dotnet build)",
                "📦 Pack Package (dotnet pack)",
                "🧪 Run Tests (dotnet test)",
                "🤖 Start Antigravity AI (ask-ai)",
                "🛸 Open Control Center (cc)",
                "❌ Cancel"
            };

            var sel = SpectreMenu.ShowWithEscape("Select Startup Command for New Terminal", options, 0);
            if (sel < 0 || sel == options.Length - 1) return;

            initialCommand = sel switch
            {
                1 => "dotnet build",
                2 => "dotnet pack",
                3 => "dotnet test",
                4 => "ask-ai",
                5 => "cc",
                _ => null
            };
        }

        try
        {
            var wt = ProcessRunner.Instance.FindOnPath("wt.exe") ?? ProcessRunner.Instance.FindOnPath("wt");
            if (!string.IsNullOrEmpty(wt))
            {
                var cmdArgs = !string.IsNullOrEmpty(initialCommand)
                    ? $"-d \"{path}\" powershell -NoExit -Command \"Set-Location -LiteralPath '{path}'; {initialCommand}\""
                    : $"-d \"{path}\"";
                Process.Start(new ProcessStartInfo(wt, cmdArgs) { UseShellExecute = true });
            }
            else
            {
                var cmdArgs = !string.IsNullOrEmpty(initialCommand)
                    ? $"-NoExit -Command \"Set-Location -LiteralPath '{path}'; {initialCommand}\""
                    : $"-NoExit -Command \"Set-Location -LiteralPath '{path}'\"";
                Process.Start(new ProcessStartInfo("powershell.exe", cmdArgs) { UseShellExecute = true });
            }
        }
        catch (Exception ex)
        {
            LogHelper.LogError("OpenNewTerminalSession failed", ex);
        }
    }

    public void ShowDiskSpace()
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
        catch (Exception ex)
        {
            LogHelper.LogError("ShowDiskSpace failed", ex);
        }
    }

    public string GetPublicIP()
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            return client.GetStringAsync("https://api.ipify.org").Result.Trim();
        }
        catch (Exception ex)
        {
            LogHelper.Log($"[SystemHelper] GetPublicIP unavailable: {ex.Message}", "DEBUG");
            return "Unavailable";
        }
    }

    public void KillPort(int port)
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

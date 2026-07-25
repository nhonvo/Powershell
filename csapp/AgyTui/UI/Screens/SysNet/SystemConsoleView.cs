using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace AgyTui.Infrastructure.Common;

public static class SystemConsoleView
{
    public static void ShowDiskSpace()
    {
        var rows = DriveInfo.GetDrives().Where(d => d.IsReady).Select(d =>
        {
            var usedPct = d.TotalSize > 0 ? Math.Round((1.0 - (double)d.AvailableFreeSpace / d.TotalSize) * 100.0, 1) : 0.0;
            var health = usedPct >= 90 ? "[red]Critical[/]" : usedPct >= 75 ? "[yellow]Warning[/]" : "[green]Healthy[/]";

            static string Fmt(long b) => b > 1_073_741_824 ? $"{Math.Round(b / 1_073_741_824.0, 2)} GB" : $"{Math.Round(b / 1_048_576.0, 2)} MB";
            return new[]
            {
                d.Name.EscapeMarkup(), d.DriveType.ToString().EscapeMarkup(), Fmt(d.TotalSize), Fmt(d.AvailableFreeSpace),$"{usedPct}%", health
            }
            ;
        }
        ).ToArray();
        SpectreTable.Render(["Drive", "Type", "Total", "Free", "Used%", "Health"], rows, markup: true);

    }

    public static string GetPublicIP()
    {
        try
        {
            var res = HttpClientProvider.Client.GetStringAsync("https://api.ipify.org").GetAwaiter().GetResult();
            return res.Trim();
        }
        catch
        {
            return "Unknown";
        }
    }

    public static bool KillPort(int port)
    {
        var result = RunProcess("netstat", $"-ano", capture: true);
        var killedAny = false;
        var seenPids = new HashSet<int>();
        foreach (var line in result.Split('\n'))
        {
            if (!line.Contains($":{port} ") && !line.Contains($":{port}\t")) continue;
            var parts = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 5) continue;
            if (!int.TryParse(parts[^1], out var pid)) continue;
            if (!seenPids.Add(pid)) continue;

            try
            {
                using var proc = Process.GetProcessById(pid);
                var name = proc.ProcessName;
                proc.Kill(entireProcessTree: true);
                SpectrePanel.Success($"Killed process '{name}' (PID {pid}) listening on port {port}.");
                killedAny = true;
            }
            catch (Exception ex)
            {
                SpectrePanel.Error($"Failed to kill PID {pid}: {ex.Message}");
            }
        }
        if (!killedAny) SpectrePanel.Warning($"No process found listening on port {port}.");
        return killedAny;

    }

    public static void OpenExplorer(string? path = null) => Process.Start(new ProcessStartInfo("explorer.exe", path ?? Directory.GetCurrentDirectory())
    {
        UseShellExecute = true
    });

    public static void OpenNewTerminalSession(string? workingDirectory = null, string? command = null, bool promptForCommand = false)
    {
        var dir = !string.IsNullOrEmpty(workingDirectory) && Directory.Exists(workingDirectory)
            ? workingDirectory
            : Directory.GetCurrentDirectory();

        var targetCommand = command;
        if (string.IsNullOrEmpty(targetCommand) && promptForCommand)
        {
            var options = new[]
            {
                "💻 Blank Shell (No command)",
                "🔨 Build Project (dotnet build)",
                "📦 Pack Package (dotnet pack)",
                "🧪 Run Tests (dotnet test)",
                "🤖 Start Antigravity AI (ask-ai)",
                "🛸 Open Control Center (cc)",
                "❌ Cancel"
            };

            var choice = SpectreMenu.Show("Select Startup Command for New Terminal", options, 0);
            if (choice < 0 || choice == 6) return; // Cancel or Escape

            targetCommand = choice switch
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
            var commandArg = string.IsNullOrEmpty(targetCommand) ? "" : $"-p \"PowerShell\" pwsh.exe -NoExit -Command \"{targetCommand}\"";
            var args = string.IsNullOrEmpty(targetCommand) ? $"-d \"{dir}\"" : $"-d \"{dir}\" {commandArg}";
            var psi = new ProcessStartInfo
            {
                FileName = "wt.exe",
                Arguments = args,
                UseShellExecute = true
            };
            Process.Start(psi);
            SpectrePanel.Success($"Launched Windows Terminal in: {dir}");
        }
        catch
        {
            try
            {
                var args = "-NoExit";
                if (!string.IsNullOrEmpty(targetCommand))
                {
                    args = $"-NoExit -Command \"{targetCommand}\"";
                }
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = args,
                    WorkingDirectory = dir,
                    UseShellExecute = true
                };
                Process.Start(psi);
                SpectrePanel.Success($"Launched PowerShell window in: {dir}");
            }
            catch (Exception ex)
            {
                SpectrePanel.Error($"Failed to launch new terminal session: {ex.Message}");
            }
        }
    }

    public static void StopProcessFriendly(string? name = null)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            var named = Process.GetProcessesByName(name);
            if (named.Length == 0)
            {
                SpectrePanel.Warning($"No process named '{name}' found.");
                return;
            }
            foreach (var p in named)
            {
                using (p)
                {
                    try
                    {
                        p.Kill();
                        SpectrePanel.Success($"Stopped '{p.ProcessName}' (PID {p.Id}).");
                    }
                    catch (Exception ex)
                    {
                        SpectrePanel.Error($"Failed to stop PID {p.Id}: {ex.Message}");
                    }
                }
            }
            return;
        }
        var all = Process.GetProcesses().OrderBy(p => p.ProcessName).ToArray();
        try
        {
            var labels = all.Select(p => $"{p.ProcessName,-30} PID {p.Id}").ToArray();
            var idx = SpectreMenu.Show("Select process to kill", labels, 0, true);
            if (idx >= 0)
            {
                var target = all[idx];
                try
                {
                    target.Kill();
                    SpectrePanel.Success($"Stopped '{target.ProcessName}' (PID {target.Id}).");
                }
                catch (Exception ex)
                {
                    SpectrePanel.Error($"Failed to stop PID {target.Id}: {ex.Message}");
                }
            }
        }
        finally
        {
            foreach (var p in all)
            {
                try { p.Dispose(); } catch { }
            }
        }

    }

    public static void SystemMonitor()
    {
        AnsiConsole.MarkupLine("[dim]Press Escape or Enter to exit System Monitor...[/]");
        PerformanceCounter? cpuCounter = null;
        PerformanceCounter? diskCounter = null;

        try
        {
            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            diskCounter = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total");
            cpuCounter.NextValue();
            diskCounter.NextValue();
        }
        catch
        {
        }
        try
        {
            while (true)
            {
                var cpu = 0.0;

                try
                {
                    cpu = cpuCounter?.NextValue() ?? 0.0;
                }
                catch
                {
                }
                var disk = 0.0;

                try
                {
                    disk = Math.Min(100.0, diskCounter?.NextValue() ?? 0.0);
                }
                catch
                {
                }
                GetMemoryInfo(out var totalMb, out var availMb);
                var usedMb = totalMb - availMb;
                var ramPercent = totalMb > 0 ? (usedMb / totalMb) * 100.0 : 0.0;
                AnsiConsole.MarkupLine($" CPU Usage: {Bar(cpu)} {cpu:F1}%".PadRight(60));
                AnsiConsole.MarkupLine($" RAM Usage: {Bar(ramPercent)} {ramPercent:F1}% ({usedMb / 1024.0:F2} GB / {totalMb / 1024.0:F2} GB)".PadRight(60));
                AnsiConsole.MarkupLine($" Disk I/O: {Bar(disk)} {disk:F1}%".PadRight(60));
                var exit = false;
                for (var s = 0;
                s < 20;
                s++)
                {
                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(true);
                        if (key.Key is ConsoleKey.Escape or ConsoleKey.Enter)
                        {
                            exit = true;
                            break;
                        }
                    }
                    Thread.Sleep(100);
                }
                if (exit) break;
                AnsiConsole.Cursor.MoveUp(3);
            }
        }
        finally
        {
            cpuCounter?.Dispose();
            diskCounter?.Dispose();
        }

    }

    private static string Bar(double percentage)
    {
        var filled = Math.Clamp((int)Math.Round(percentage / 100.0 * 20), 0, 20);
        return "[" + new string('█', filled) + new string('░', 20 - filled) + "]";

    }
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MemoryStatusEx
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;

    }

    private static void GetMemoryInfo(out double totalMb, out double availMb)
    {
        var status = new MemoryStatusEx();
        status.dwLength = (uint)Marshal.SizeOf<MemoryStatusEx>();
        if (GlobalMemoryStatusEx(ref status))
        {
            totalMb = status.ullTotalPhys / 1024.0 / 1024.0;
            availMb = status.ullAvailPhys / 1024.0 / 1024.0;
        }
        else
        {
            totalMb = 1.0;
            availMb = 1.0;
        }

    }

    public static void ShowSshConnectionInfo()
    {
        var localIPs = NetworkInterface.GetAllNetworkInterfaces().Where(n => n.OperationalStatus == OperationalStatus.Up).SelectMany(n => n.GetIPProperties().UnicastAddresses).Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(a.Address)).Select(a => a.Address.ToString()).ToArray();
        AnsiConsole.Write(new Rule("[bold cyan]SSH Connection Info[/]").RuleStyle("grey"));
        var ipRows = localIPs.Select(ip => new[]
        {
            ip
        }
        ).ToArray();
        SpectreTable.Render(["Local IPv4"], ipRows);
        var tailscaleIP = Environment.GetEnvironmentVariable("TAILSCALE_IP") ?? "Not configured";
        AnsiConsole.MarkupLine($" Tailscale: [cyan]{tailscaleIP.EscapeMarkup()}[/]");
        var netstatOut = RunProcess("netstat", "-an", capture: true);
        var sshConns = netstatOut.Split('\n').Where(l => l.Contains(":22 ") || l.Contains(":22\t")).Where(l => l.Contains("ESTABLISHED")).ToArray();
        AnsiConsole.MarkupLine($" Active SSH connections: [yellow]{sshConns.Length}[/]");
        foreach (var c in sshConns) AnsiConsole.MarkupLine($" [dim]{c.Trim().EscapeMarkup()}[/]");

    }

    internal static string RunProcess(string exe, string args, bool capture = false)
    {
        if (capture)
        {
            return ProcessRunner.RunCapture(exe, args);
        }
        else
        {
            ProcessRunner.Run(exe, args);
            return string.Empty;
        }
    }

    public static bool IsFuzzyMatch(string source, string query)
    {
        if (string.IsNullOrEmpty(query)) return true;
        if (string.IsNullOrEmpty(source)) return false;
        int sourceIdx = 0;
        int queryIdx = 0;
        while (sourceIdx < source.Length && queryIdx < query.Length)
        {
            if (char.ToLowerInvariant(source[sourceIdx]) == char.ToLowerInvariant(query[queryIdx]))
            {
                queryIdx++;
            }
            sourceIdx++;
        }
        return queryIdx == query.Length;
    }

    public static string BoldFuzzyMatch(string source, string query)
    {
        if (string.IsNullOrEmpty(query)) return source.EscapeMarkup();

        var sb = new System.Text.StringBuilder();
        int queryIdx = 0;

        for (int i = 0; i < source.Length; i++)
        {
            var ch = source[i];
            if (queryIdx < query.Length && char.ToLowerInvariant(ch) == char.ToLowerInvariant(query[queryIdx]))
            {
                sb.Append($"[bold green]{ch.ToString().EscapeMarkup()}[/]");
                queryIdx++;
            }
            else
            {
                sb.Append(ch.ToString().EscapeMarkup());
            }
        }
        return sb.ToString();
    }
}


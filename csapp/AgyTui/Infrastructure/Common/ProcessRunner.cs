using System.Diagnostics;
using System.Text;

namespace AgyTui.Infrastructure.Common;

public class ProcessRunner : IProcessRunner
{
    private static readonly Lazy<ProcessRunner> _instance = new(() => new ProcessRunner());
    public static ProcessRunner Instance => _instance.Value;

    public string? FindOnPath(string exe)
    {
        try
        {
            var cmd = OperatingSystem.IsWindows() ? "where" : "which";
            var psi = new ProcessStartInfo(cmd, exe)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var p = Process.Start(psi);
            if (p == null) return null;
            var output = p.StandardOutput.ReadToEnd().Trim();
            if (!p.WaitForExit(3000))
            {
                try { p.Kill(entireProcessTree: true); } catch (Exception) { }
                return null;
            }
            if (p.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
            {
                var lines = output.Replace("\r", "").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                return lines.FirstOrDefault();
            }
        }
        catch (Exception)
        {
        }
        return null;
    }

    public void RunInteractive(string exe, IEnumerable<string> args, IDictionary<string, string?>? env = null, string? workingDir = null)
    {
        var resolvedExe = Path.IsPathRooted(exe) ? exe : FindOnPath(exe) ?? exe;
        var targetWorkingDir = !string.IsNullOrEmpty(workingDir) && Directory.Exists(workingDir)
            ? workingDir
            : Directory.GetCurrentDirectory();

        ProcessStartInfo psi;
        if (OperatingSystem.IsWindows() && (resolvedExe.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase) || resolvedExe.EndsWith(".bat", StringComparison.OrdinalIgnoreCase) || resolvedExe.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase)))
        {
            psi = new ProcessStartInfo("cmd.exe")
            {
                UseShellExecute = false,
                WorkingDirectory = targetWorkingDir
            };
            psi.ArgumentList.Add("/c");
            psi.ArgumentList.Add(resolvedExe);
            foreach (var a in args)
            {
                psi.ArgumentList.Add(a);
            }
        }
        else
        {
            psi = new ProcessStartInfo(resolvedExe)
            {
                UseShellExecute = false,
                WorkingDirectory = targetWorkingDir
            };
            foreach (var a in args)
            {
                psi.ArgumentList.Add(a);
            }
        }

        if (env != null)
        {
            foreach (var kv in env)
            {
                if (kv.Value == null)
                    psi.Environment.Remove(kv.Key);
                else
                    psi.Environment[kv.Key] = kv.Value;
            }
        }
        try
        {
            using var p = Process.Start(psi);
            p?.WaitForExit();
        }
        catch (Exception ex)
        {
            SpectrePanel.Error($"Failed to launch '{exe}': {ex.Message}");
        }
    }

    public string RunCapture(string exe, string args, string? workingDir = null)
    {
        var (stdout, _, _) = RunCaptureWithDetails(exe, args, workingDir, TimeSpan.FromSeconds(30));
        return stdout;
    }

    public (string Stdout, string Stderr, int ExitCode) RunCaptureWithDetails(
        string exe, string args, string? workingDir = null, TimeSpan? timeout = null)
    {
        var resolvedExe = Path.IsPathRooted(exe) ? exe : FindOnPath(exe) ?? exe;
        var targetWorkingDir = !string.IsNullOrEmpty(workingDir) && Directory.Exists(workingDir)
            ? workingDir
            : Directory.GetCurrentDirectory();

        ProcessStartInfo psi;
        if (OperatingSystem.IsWindows() && (resolvedExe.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase) || resolvedExe.EndsWith(".bat", StringComparison.OrdinalIgnoreCase) || resolvedExe.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase)))
        {
            psi = new ProcessStartInfo("cmd.exe", $"/c \"{resolvedExe}\" {args}")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = targetWorkingDir
            };
        }
        else
        {
            psi = new ProcessStartInfo(resolvedExe, args)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = targetWorkingDir
            };
        }

        var stdoutBuilder = new StringBuilder();
        var stderrBuilder = new StringBuilder();

        try
        {
            using var p = new Process();
            p.StartInfo = psi;
            p.OutputDataReceived += (_, e) => { if (e.Data != null) stdoutBuilder.AppendLine(e.Data); };
            p.ErrorDataReceived += (_, e) => { if (e.Data != null) stderrBuilder.AppendLine(e.Data); };

            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();

            var limit = timeout ?? TimeSpan.FromSeconds(30);
            if (p.WaitForExit((int)limit.TotalMilliseconds))
            {
                p.WaitForExit();
                return (stdoutBuilder.ToString(), stderrBuilder.ToString(), p.ExitCode);
            }
            else
            {
                try { p.Kill(true); } catch (Exception) { }
                return (stdoutBuilder.ToString(), stderrBuilder.ToString() + "\n[TIMED OUT]", -1);
            }
        }
        catch (Exception ex)
        {
            return ("", ex.Message, -1);
        }
    }

    public int Run(string exe, string args, string? workingDir = null, TimeSpan? timeout = null)
    {
        string realExe = exe.Trim();
        string realArgs = args;
        if (realExe.Contains(' '))
        {
            int spaceIdx = realExe.IndexOf(' ');
            realArgs = realExe[(spaceIdx + 1)..].Trim() + " " + realArgs;
            realExe = realExe[..spaceIdx].Trim();
        }

        var psi = new ProcessStartInfo(realExe, realArgs)
        {
            UseShellExecute = false,
            WorkingDirectory = workingDir ?? Directory.GetCurrentDirectory()
        };

        try
        {
            using var p = Process.Start(psi);
            if (p == null) return -1;
            var timeoutMs = (int)(timeout?.TotalMilliseconds ?? 30000);
            if (p.WaitForExit(timeoutMs)) return p.ExitCode;
            try { p.Kill(entireProcessTree: true); } catch { }
            return -1;
        }
        catch
        {
            return -1;
        }
    }

    public string RunCapture(string exe, IEnumerable<string> args, string? workingDir = null)
    {
        var (stdout, _, _) = RunCaptureWithDetails(exe, args, workingDir, TimeSpan.FromSeconds(30));
        return stdout;
    }

    public (string Stdout, string Stderr, int ExitCode) RunCaptureWithDetails(
        string exe, IEnumerable<string> args, string? workingDir = null, TimeSpan? timeout = null)
    {
        var psi = new ProcessStartInfo(exe)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDir ?? Directory.GetCurrentDirectory()
        };
        foreach (var a in args)
        {
            psi.ArgumentList.Add(a);
        }

        var stdoutBuilder = new StringBuilder();
        var stderrBuilder = new StringBuilder();

        try
        {
            using var p = new Process { StartInfo = psi };
            p.OutputDataReceived += (s, e) => { if (e.Data != null) stdoutBuilder.AppendLine(e.Data); };
            p.ErrorDataReceived += (s, e) => { if (e.Data != null) stderrBuilder.AppendLine(e.Data); };

            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();

            var limit = timeout ?? TimeSpan.FromSeconds(30);
            if (p.WaitForExit((int)limit.TotalMilliseconds))
            {
                p.WaitForExit();
                return (stdoutBuilder.ToString(), stderrBuilder.ToString(), p.ExitCode);
            }
            else
            {
                try { p.Kill(true); } catch { }
                return (stdoutBuilder.ToString(), stderrBuilder.ToString() + "\n[TIMED OUT]", -1);
            }
        }
        catch (Exception ex)
        {
            return ("", ex.Message, -1);
        }
    }

    public string RunCapture(string exe, string[] args, string? workingDir = null)
    {
        return RunCapture(exe, (IEnumerable<string>)args, workingDir);
    }

    public int Run(string exe, string[] args, string? workingDir = null)
    {
        var psi = new ProcessStartInfo(exe)
        {
            UseShellExecute = false,
            WorkingDirectory = workingDir ?? Directory.GetCurrentDirectory()
        };
        foreach (var a in args)
        {
            psi.ArgumentList.Add(a);
        }
        try
        {
            using var p = Process.Start(psi);
            if (p == null) return -1;
            if (!p.WaitForExit(30000))
            {
                try { p.Kill(entireProcessTree: true); } catch { }
                return -1;
            }
            return p.ExitCode;
        }
        catch { return -1; }
    }

}

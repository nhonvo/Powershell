using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using AgyTui.Components;

namespace AgyTui.Helpers;

public static class ProcessRunner
{
    public static string? FindOnPath(string exe)
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
                try { p.Kill(entireProcessTree: true); } catch { }
                return null;
            }
            if (p.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
            {
                var lines = output.Replace("\r", "").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                return lines.FirstOrDefault();
            }
        }
        catch
        {
        }
        return null;
    }

    public static void RunInteractive(string exe, IEnumerable<string> args, IDictionary<string, string?>? env = null, string? workingDir = null)
    {
        var resolvedExe = Path.IsPathRooted(exe) ? exe : FindOnPath(exe) ?? exe;
        var psi = new ProcessStartInfo(resolvedExe)
        {
            UseShellExecute = false,
            WorkingDirectory = workingDir ?? Directory.GetCurrentDirectory()
        };
        foreach (var a in args)
        {
            psi.ArgumentList.Add(a);
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

    public static string RunCapture(string exe, string args, string? workingDir = null)
    {
        var (stdout, _, _) = RunCaptureWithDetails(exe, args, workingDir, TimeSpan.FromSeconds(30));
        return stdout;
    }

    public static (string Stdout, string Stderr, int ExitCode) RunCaptureWithDetails(
        string exe, string args, string? workingDir = null, TimeSpan? timeout = null)
    {
        var psi = new ProcessStartInfo(exe, args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDir ?? Directory.GetCurrentDirectory()
        };

        var stdoutBuilder = new System.Text.StringBuilder();
        var stderrBuilder = new System.Text.StringBuilder();

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

    public static int Run(string exe, string args, string? workingDir = null, TimeSpan? timeout = null)
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
            int timeoutMs = (int)(timeout?.TotalMilliseconds ?? 30000);
            if (!p.WaitForExit(timeoutMs))
            {
                try { p.Kill(entireProcessTree: true); } catch { }
                return -1;
            }
            return p.ExitCode;
        }
        catch
        {
            return -1;
        }
    }

    public static string RunCapture(string exe, string[] args, string? workingDir = null)
    {
        return RunCapture(exe, string.Join(" ", args), workingDir);
    }

    public static int Run(string exe, string[] args, string? workingDir = null)
    {
        return Run(exe, string.Join(" ", args), workingDir);
    }
}

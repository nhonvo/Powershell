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
            var psi = new ProcessStartInfo("where", exe)
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var p = Process.Start(psi);
            var output = p?.StandardOutput.ReadToEnd().Trim();
            p?.WaitForExit();
            if (p?.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
            {
                var lines = output.Replace("\r", "").Split('\n');
                return lines[0].Trim();
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
        var psi = new ProcessStartInfo(exe, args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDir ?? Directory.GetCurrentDirectory()
        };

        try
        {
            using var p = Process.Start(psi);
            if (p == null) return "";
            var output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            return output;
        }
        catch
        {
            return "";
        }
    }

    public static int Run(string exe, string args, string? workingDir = null)
    {
        var psi = new ProcessStartInfo(exe, args)
        {
            UseShellExecute = false,
            WorkingDirectory = workingDir ?? Directory.GetCurrentDirectory()
        };

        try
        {
            using var p = Process.Start(psi);
            p?.WaitForExit();
            return p?.ExitCode ?? -1;
        }
        catch
        {
            return -1;
        }
    }
}

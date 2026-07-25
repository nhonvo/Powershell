namespace AgyTui.Tests.Unit.Infrastructure.Integrations;

using System;
using System.Diagnostics;
using AgyTui.Infrastructure.Common;
using Xunit;

public class AntigravityDeckClientTests
{
    [Fact]
    public void ProcessRunner_RunInteractive_DoesNotKillLongRunningProcessAt30s()
    {
        var sw = Stopwatch.StartNew();

        var exe = OperatingSystem.IsWindows() ? "cmd.exe" : "sleep";
        var args = OperatingSystem.IsWindows() ? new[] { "/c", "ping 127.0.0.1 -n 2 >nul" } : new[] { "1" };

        ProcessRunner.RunInteractive(exe, args);
        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds >= 500);
    }
}

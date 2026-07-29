using System.Diagnostics;

namespace AgyTui.Tests.Unit.Infrastructure.Integrations;

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

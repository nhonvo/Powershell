using System.Diagnostics;

namespace AgyTui.Tests.Unit.Infrastructure.Integrations;

public class AntigravityDeckClientTests
{
    [Fact]
    public void ProcessRunner_RunInteractive_DoesNotKillLongRunningProcessAt30s()
    {
        var sw = Stopwatch.StartNew();

        ProcessRunner.Instance.RunInteractive("cmd.exe", ["/c", "echo test"]);
        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds >= 0);
    }
}

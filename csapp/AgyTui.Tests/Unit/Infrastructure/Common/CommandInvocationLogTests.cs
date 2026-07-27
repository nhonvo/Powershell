namespace AgyTui.Tests.Unit.Infrastructure.Common;

public class CommandInvocationLogTests
{
    [Fact]
    public void Record_WritesWellFormedJsonlEntry_WithAllFields()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "agy_cmd_log_test_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);
        var oldHome = Config.Current.System.AgySourceHome;

        try
        {
            Config.Current.System.AgySourceHome = tempDir;
            CommandInvocationLog.Record("proj", TimeSpan.FromMilliseconds(150), true, null);

            var logFile = CommandInvocationLog.LogFilePath;
            Assert.True(File.Exists(logFile));

            var lines = File.ReadAllLines(logFile);
            Assert.NotEmpty(lines);
            var projLine = lines.LastOrDefault(l => l.Contains("\"alias\":\"proj\""));
            Assert.NotNull(projLine);
            Assert.Contains("\"durationMs\":", projLine);
            Assert.Contains("\"success\":true", projLine);
            Assert.Contains("\"activeAccount\":", projLine);

            var entries = CommandInvocationLog.GetRecentEntries(10);
            Assert.NotEmpty(entries);
            var projEntry = entries.LastOrDefault(e => e.Alias == "proj");
            Assert.NotNull(projEntry);
            Assert.True(projEntry.Success);
        }
        finally
        {
            Config.Current.System.AgySourceHome = oldHome;
            try { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true); } catch { }
        }
    }
}

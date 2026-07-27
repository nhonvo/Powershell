namespace AgyTui.Tests.Unit.Infrastructure.Common;

using System;
using System.IO;
using AgyTui.Infrastructure.Common;
using Xunit;

public class CommandInvocationLogTests
{
    [Fact]
    public void Record_WritesWellFormedJsonlEntry_WithAllSixFields()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "agy_cmd_log_test_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);
        var oldHome = Config.Current.AgySourceHome;

        try
        {
            Config.Current.AgySourceHome = tempDir;
            CommandInvocationLog.Record("proj", TimeSpan.FromMilliseconds(150), true, null);

            var logFile = CommandInvocationLog.LogFilePath;
            Assert.True(File.Exists(logFile));

            var lines = File.ReadAllLines(logFile);
            Assert.NotEmpty(lines);
            var projLine = lines.LastOrDefault(l => l.Contains("\"alias\":\"proj\""));
            Assert.NotNull(projLine);
            Assert.Contains("\"durationMs\":", projLine);
            Assert.Contains("\"success\":true", projLine);
        }
        finally
        {
            Config.Current.AgySourceHome = oldHome;
            try { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true); } catch { }
        }
    }
}

namespace AgyTui.Tests.Unit.Architecture;

using System.IO;
using Xunit;

public class RepoHygieneTests
{
    [Fact]
    public void AssetDirectories_WhenPresent_AreValidDirectories()
    {
        var baseDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."));
        var psappDir = Path.Combine(baseDir, "psapp");
        Assert.True(Directory.Exists(psappDir), $"psapp directory missing: {psappDir}");
    }
}

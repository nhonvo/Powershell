namespace AgyTui.Tests.Unit.Architecture;

public class RepoHygieneTests
{
    [Fact]
    public void AssetDirectories_WhenPresent_AreValidDirectories()
    {
        var baseDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", ".."));
        if (!Directory.Exists(Path.Combine(baseDir, "shell")))
        {
            baseDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."));
        }
        var valid = Directory.Exists(Path.Combine(baseDir, "shell")) || Directory.Exists(Path.Combine(baseDir, "psapp"));
        Assert.True(valid, $"shell or psapp directory missing from base: {baseDir}");
    }
}

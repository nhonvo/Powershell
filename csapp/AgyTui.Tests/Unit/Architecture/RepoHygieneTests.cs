namespace AgyTui.Tests.Unit.Architecture;

using System.IO;
using Xunit;

public class RepoHygieneTests
{
    [Fact]
    public void OrphanedAssetsDirectories_DoNotExist()
    {
        var baseDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."));
        var imgDir = Path.Combine(baseDir, "psapp", "asset", "img");
        var typoraDir = Path.Combine(baseDir, "psapp", "asset", "typora-themes");

        Assert.False(Directory.Exists(imgDir), $"Orphaned directory still exists: {imgDir}");
        Assert.False(Directory.Exists(typoraDir), $"Orphaned directory still exists: {typoraDir}");
    }
}


using AgyTui.UI.Screens.Ide;

namespace AgyTui.Tests.Unit.UI.Screens.Ide;

public class GitDiffViewerTests
{
    [Fact]
    public void BuildDiffArgs_WithSpacesInPath_QuotesFilePath()
    {
        var path = @"C:\My Folder\file.cs";
        var args = GitDiffViewer.BuildDiffArgs(path);

        Assert.Equal(@"diff ""C:\My Folder\file.cs""", args);
    }

    [Fact]
    public void BuildDiffArgs_WithNullPath_ReturnsBareDiff()
    {
        var args = GitDiffViewer.BuildDiffArgs(null);
        Assert.Equal("diff", args);
    }
}

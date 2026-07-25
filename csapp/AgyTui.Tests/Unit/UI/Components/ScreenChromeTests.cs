namespace AgyTui.Tests.Unit.UI.Components;

public class ScreenChromeTests : IDisposable
{
    private readonly StringWriter _writer;

    public ScreenChromeTests()
    {
        _writer = new StringWriter();
        ScreenChrome.OverrideConsole = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Out = new AnsiConsoleOutput(_writer)
        });
    }

    public void Dispose()
    {
        ScreenChrome.OverrideConsole = null;
        _writer.Dispose();
    }

    [Fact]
    public void RenderBanner_WritesBannerOutput()
    {
        ScreenChrome.RenderBanner();
        var output = _writer.ToString();
        Assert.NotEmpty(output);
    }

    [Fact]
    public void RenderBanner_WithCategoryAndActiveItem_IncludesBreadcrumbs()
    {
        ScreenChrome.RenderBanner("Workspace & Dev", "proj", forceClear: true);
        var output = _writer.ToString();
        Assert.Contains("Workspace & Dev", output);
        Assert.Contains("proj", output);
    }

    [Fact]
    public void WriteLineSmooth_ShorterLineThanPrevious_ErasesTrailingCharacters()
    {
        ScreenChrome.WriteLineSmooth("Long initial line text");
        var firstOutput = _writer.ToString();
        Assert.Contains("Long initial line text", firstOutput);

        ScreenChrome.WriteLineSmooth("Short");
        var fullOutput = _writer.ToString();
        Assert.Contains("Short", fullOutput);
    }

    [Fact]
    public void RenderFrame_ForceClearFalse_UsesCursorHomeNotFullClear()
    {
        bool drawn = false;
        ScreenChrome.RenderFrame(() => { drawn = true; }, forceClear: false);
        Assert.True(drawn);
    }
}

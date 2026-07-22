using System;
using System.IO;
using AgyTui;
using Spectre.Console;
using Xunit;

namespace AgyTuiApp.Tests;

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
}

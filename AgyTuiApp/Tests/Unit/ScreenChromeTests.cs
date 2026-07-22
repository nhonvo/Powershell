using System;
using AgyTui;
using Xunit;

namespace AgyTuiApp.Tests;

public class ScreenChromeTests
{
    [Fact]
    public void RenderBanner_DoesNotThrowException()
    {
        var exception = Record.Exception(() => ScreenChrome.RenderBanner());
        Assert.Null(exception);
    }

    [Fact]
    public void RenderBanner_WithCategoryAndActiveItem_DoesNotThrowException()
    {
        var exception = Record.Exception(() => ScreenChrome.RenderBanner("Workspace & Dev", "proj", forceClear: true));
        Assert.Null(exception);
    }
}

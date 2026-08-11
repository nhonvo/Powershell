using AgyTui.Infrastructure.Di;
using AgyTui.UI.Core.Layouts;
using AgyTui.UI.Core.Layouts.Abstractions;
using AgyTui.UI.Core.Abstractions;
using AgyTui.UI.Screens.Customization.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace AgyTui.Tests.Unit.UI.Core;

public class LayoutsTests
{
    [Fact]
    public void LayoutInterfaces_ResolveFromDI_Successfully()
    {
        var screenChrome = Bootstrapper.ServiceProvider.GetRequiredService<IScreenChrome>();
        Assert.NotNull(screenChrome);

        var profileHelp = Bootstrapper.ServiceProvider.GetRequiredService<IProfileHelp>();
        Assert.NotNull(profileHelp);

        var nodeBuilder = Bootstrapper.ServiceProvider.GetRequiredService<IMenuNodeBuilder>();
        Assert.NotNull(nodeBuilder);
    }

    [Fact]
    public void MenuNodeBuilder_BuildTree_ReturnsValidMenuHierarchy()
    {
        var nodeBuilder = Bootstrapper.ServiceProvider.GetRequiredService<IMenuNodeBuilder>();
        var root = nodeBuilder.BuildTree();

        Assert.NotNull(root);
        Assert.Equal("root", root.Id);
        Assert.NotEmpty(root.Children);
    }

    [Fact]
    public void ScreenChrome_FormattingMethods_ReturnMarkup()
    {
        var chrome = Bootstrapper.ServiceProvider.GetRequiredService<IScreenChrome>();

        var accent = chrome.Accent("test");
        Assert.Contains("cyan", accent);

        var success = chrome.Success("test");
        Assert.Contains("green", success);

        var warning = chrome.Warning("test");
        Assert.Contains("yellow", warning);

        var error = chrome.Error("test");
        Assert.Contains("red", error);
    }
}

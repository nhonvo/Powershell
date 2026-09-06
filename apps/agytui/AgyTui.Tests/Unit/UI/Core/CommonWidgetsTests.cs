using AgyTui.Infrastructure.Di;
using AgyTui.UI.Core.Components;
using AgyTui.UI.Core.Abstractions;
using AgyTui.UI.Core.Components.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace AgyTui.Tests.Unit.UI.Core;

public class CommonWidgetsTests
{
    [Fact]
    public void UiCoreInterfaces_ResolveFromDI_Successfully()
    {
        var components = Bootstrapper.ServiceProvider.GetRequiredService<IAgyUiComponents>();
        Assert.NotNull(components);

        var icons = Bootstrapper.ServiceProvider.GetRequiredService<IIcons>();
        Assert.NotNull(icons);

        var scroll = Bootstrapper.ServiceProvider.GetRequiredService<IScrollableListView>();
        Assert.NotNull(scroll);

        var menu = Bootstrapper.ServiceProvider.GetRequiredService<ISpectreMenu>();
        Assert.NotNull(menu);

        var panel = Bootstrapper.ServiceProvider.GetRequiredService<ISpectrePanel>();
        Assert.NotNull(panel);

        var table = Bootstrapper.ServiceProvider.GetRequiredService<ISpectreTable>();
        Assert.NotNull(table);

        var progress = Bootstrapper.ServiceProvider.GetRequiredService<ISpectreProgress>();
        Assert.NotNull(progress);

        var widgetRegistry = Bootstrapper.ServiceProvider.GetRequiredService<IStatusWidgetRegistry>();
        Assert.NotNull(widgetRegistry);
    }

    [Fact]
    public void Icons_GetFileIcon_And_CategoryIcon_ReturnExpectedGlyphs()
    {
        var icons = Bootstrapper.ServiceProvider.GetRequiredService<IIcons>();

        var csIcon = icons.GetFileIcon(".cs");
        Assert.False(string.IsNullOrEmpty(csIcon));

        var catIcon = icons.GetCategoryIcon("workspace");
        Assert.False(string.IsNullOrEmpty(catIcon));

        var statusIcon = icons.GetStatusIcon("running");
        Assert.Equal("🟢", statusIcon);
    }

    [Fact]
    public void ScrollableListView_ComputeViewport_CalculatesCorrectTopAndEndRows()
    {
        var listView = Bootstrapper.ServiceProvider.GetRequiredService<IScrollableListView>();

        var (topRow, endRow) = listView.ComputeViewport(50, 25, 10);
        Assert.True(topRow >= 0);
        Assert.True(endRow <= 50);
        Assert.Equal(10, endRow - topRow);

        var step = listView.GetPageStep(10);
        Assert.Equal(5, step);
    }

    [Fact]
    public void StatusWidgetRegistry_GetAll_ReturnsWidgets()
    {
        var registry = Bootstrapper.ServiceProvider.GetRequiredService<IStatusWidgetRegistry>();
        var widgets = registry.GetAll();
        Assert.NotEmpty(widgets);

        var diskWidget = registry.GetByAlias("disk");
        Assert.NotNull(diskWidget);
    }
}


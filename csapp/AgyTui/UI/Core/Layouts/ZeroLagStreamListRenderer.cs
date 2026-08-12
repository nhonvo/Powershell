using Spectre.Console.Rendering;

namespace AgyTui.UI.Core.Layouts;

public sealed class ZeroLagStreamListRenderer : ILayoutRenderer
{
    public IRenderable Render(IScreenView view, ScreenState state)
    {
        var grid = new Grid().AddColumn(new GridColumn().NoWrap());
        var content = view.Render(grid, state);
        var footer = FooterTitleBar.Render(view.Category, view.Title, "↑/↓ Select │ Enter Launch │ Esc Back", state.SearchFilter);

        var mainGrid = new Grid().AddColumn(new GridColumn().NoWrap());
        mainGrid.AddRow(content);
        mainGrid.AddRow(footer);
        return mainGrid;
    }
}

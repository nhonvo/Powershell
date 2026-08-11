using Spectre.Console;
using Spectre.Console.Rendering;

namespace AgyTui.UI.Core.Layouts;

public sealed class DualPaneExplorerRenderer : ILayoutRenderer
{
    public IRenderable Render(IScreenView view, ScreenState state)
    {
        var grid = new Grid()
            .AddColumn(new GridColumn().Width(35))
            .AddColumn(new GridColumn());
            
        var content = view.Render(grid, state);
        var footer = FooterTitleBar.Render(view.Category, view.Title, "Tab Switch Pane │ ↑/↓ Scroll │ / Search │ Esc Exit", state.SearchFilter);

        var container = new Grid().AddColumn(new GridColumn().NoWrap());
        container.AddRow(content);
        container.AddRow(footer);
        return container;
    }
}

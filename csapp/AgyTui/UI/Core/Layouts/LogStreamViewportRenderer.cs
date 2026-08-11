using Spectre.Console;
using Spectre.Console.Rendering;

namespace AgyTui.UI.Core.Layouts;

public sealed class LogStreamViewportRenderer : ILayoutRenderer
{
    public IRenderable Render(IScreenView view, ScreenState state)
    {
        var grid = new Grid().AddColumn(new GridColumn().NoWrap());
        var content = view.Render(grid, state);
        var footer = FooterTitleBar.Render(view.Category, view.Title, "PgUp/PgDn Scroll │ f Tail │ / Filter │ Esc Back", state.SearchFilter);

        var container = new Grid().AddColumn(new GridColumn().NoWrap());
        container.AddRow(content);
        container.AddRow(footer);
        return container;
    }
}

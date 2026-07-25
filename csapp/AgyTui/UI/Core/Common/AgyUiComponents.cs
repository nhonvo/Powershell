using Spectre.Console.Rendering;

namespace AgyTui.UI.Core.Common;

public static class AgyUiComponents
{
    public static IRenderable RenderFilter(string searchBuffer, bool active)
    {
        var text = string.IsNullOrEmpty(searchBuffer)
            ? "[dim]Type / or start typing to filter...[/]"
            : $"🔍 [bold white]{searchBuffer.EscapeMarkup()}[/][blink green]_[/]";

        var borderStyle = new Style(active ? AgyThemeColors.GetSelectedColor() : AgyThemeColors.GetBorderColor());

        return new Panel(new Markup(text))
        {
            Border = BoxBorder.Rounded,
            BorderStyle = borderStyle,
            Header = new PanelHeader($"[bold {AgyThemeColors.Accent}] Filter [/]")
        };
    }

    public static IRenderable RenderScrollIndicator(int totalCount, int topRow, int endRow, int maxRows)
    {
        if (totalCount <= maxRows)
        {
            return new Markup($"  [dim]▲ Start of list   ·   ▼ End of list ({totalCount} items)[/]");
        }

        var percentStart = (double)topRow / totalCount;
        var percentVisible = (double)(endRow - topRow) / totalCount;

        const int barLength = 20;
        int activeStart = (int)Math.Round(percentStart * barLength);
        int activeLength = (int)Math.Round(percentVisible * barLength);
        if (activeLength < 1) activeLength = 1;
        if (activeStart + activeLength > barLength) activeStart = barLength - activeLength;

        var sb = new StringBuilder();
        sb.Append($"  [{AgyThemeColors.Accent}]▲ {topRow} above[/]  ");

        for (int i = 0; i < barLength; i++)
        {
            if (i >= activeStart && i < activeStart + activeLength)
            {
                sb.Append($"[{AgyThemeColors.Selected}]█[/]");
            }
            else
            {
                sb.Append("[grey]░[/]");
            }
        }

        int remaining = totalCount - endRow;
        sb.Append($"  [{AgyThemeColors.Accent}]▼ {remaining} below[/]");

        return new Markup(sb.ToString());
    }

    public static IRenderable RenderFooterNote(string noteText)
    {
        var text = string.IsNullOrWhiteSpace(noteText)
            ? "[dim]Use arrows to navigate options[/]"
            : $"[bold {AgyThemeColors.Secondary}]💡 Tip:[/] [white]{noteText.EscapeMarkup()}[/]";

        return new Panel(new Markup(text))
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(AgyThemeColors.GetBorderColor()),
            Padding = new Padding(2, 0, 2, 0)
        };
    }
}

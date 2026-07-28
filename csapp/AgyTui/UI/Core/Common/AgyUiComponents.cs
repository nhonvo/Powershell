using Spectre.Console.Rendering;

namespace AgyTui.UI.Core.Common;

public static class AgyUiComponents
{
    public static IRenderable RenderFilter(string searchBuffer, bool active)
    {
        var text = string.IsNullOrEmpty(searchBuffer)
            ? $"[bold {AgyThemeColors.Accent}]🔍 Filter:[/] [dim][[ / ]] type to filter...[/]"
            : $"[bold {AgyThemeColors.Accent}]🔍 Filter:[/] [[ [bold white]{searchBuffer.EscapeMarkup()}[/][blink green]_[/] ]]";

        return new Markup(text);
    }

    public static IRenderable RenderScrollIndicator(int totalCount, int topRow, int endRow, int maxRows)
    {
        if (totalCount <= maxRows)
        {
            return new Markup($"[dim]▲ 0 above  ░░░░░░███░░░░  ▼ 0 below[/]");
        }

        var percentStart = (double)topRow / totalCount;
        var percentVisible = (double)(endRow - topRow) / totalCount;

        const int barLength = 16;
        int activeStart = (int)Math.Round(percentStart * barLength);
        int activeLength = (int)Math.Round(percentVisible * barLength);
        if (activeLength < 1) activeLength = 1;
        if (activeStart + activeLength > barLength) activeStart = barLength - activeLength;

        var sb = new StringBuilder();
        sb.Append($"[{AgyThemeColors.Accent}]▲ {topRow} above[/]  ");

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

    public static IRenderable RenderFooterNote(string noteText, int maxLen = 100)
    {
        var rawText = string.IsNullOrWhiteSpace(noteText)
            ? "Use [↑/↓] or [j/k] to navigate options."
            : noteText;

        if (rawText.Length > maxLen) rawText = rawText[..(maxLen - 1)] + "…";

        var text = $"[bold {AgyThemeColors.Secondary}]💡 Tip:[/] [white]{rawText.EscapeMarkup()}[/]\x1b[K";
        return new Markup(text);
    }
}

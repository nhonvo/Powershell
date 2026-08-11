using Spectre.Console.Rendering;

namespace AgyTui.UI.Core.Components.Abstractions;

public interface IAgyUiComponents
{
    IRenderable RenderFilter(string searchBuffer, bool active);
    IRenderable RenderScrollIndicator(int totalCount, int topRow, int endRow, int maxRows);
    IRenderable RenderFooterNote(string noteText, int maxLen = 100);
}

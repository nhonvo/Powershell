namespace AgyTui.UI.Core.Common;

public static class ScrollableListView
{
    public static (int TopRow, int EndRow) ComputeViewport(int totalCount, int selectedIndex, int maxVisibleRows)
    {
        if (totalCount <= 0) return (0, 0);
        if (maxVisibleRows <= 0) maxVisibleRows = 10;

        if (totalCount <= maxVisibleRows) return (0, totalCount);

        // Center selectedIndex in the middle of the visible viewport
        int half = maxVisibleRows / 2;
        int topRow = selectedIndex - half;

        topRow = Math.Max(0, Math.Min(topRow, totalCount - maxVisibleRows));
        int endRow = Math.Min(totalCount, topRow + maxVisibleRows);
        return (topRow, endRow);
    }

    public static int GetPageStep(int maxVisibleRows)
    {
        return Math.Max(1, maxVisibleRows / 2);
    }

    public static string RenderAboveIndicator(int topRow)
    {
        return topRow > 0 ? $"[dim yellow]  ▲ {topRow} item(s) above...[/]" : "";
    }

    public static string RenderBelowIndicator(int endRow, int totalCount)
    {
        return endRow < totalCount ? $"[dim yellow]  ▼ {totalCount - endRow} item(s) below...[/]" : "";
    }
}

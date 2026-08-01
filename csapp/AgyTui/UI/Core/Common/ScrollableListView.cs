using AgyTui.UI.Core.Interfaces;

namespace AgyTui.UI.Core.Common;

public class ScrollableListViewService : IScrollableListView
{
    public (int TopRow, int EndRow) ComputeViewport(int totalCount, int selectedIndex, int maxVisibleRows)
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

    public int GetPageStep(int maxVisibleRows)
    {
        return Math.Max(1, maxVisibleRows / 2);
    }

    public string RenderAboveIndicator(int topRow)
    {
        return topRow > 0 ? $"[dim yellow]  ▲ {topRow} item(s) above...[/]" : "";
    }

    public string RenderBelowIndicator(int endRow, int totalCount)
    {
        return endRow < totalCount ? $"[dim yellow]  ▼ {totalCount - endRow} item(s) below...[/]" : "";
    }
}

public static class ScrollableListView
{
    private static readonly IScrollableListView _service = new ScrollableListViewService();
    public static IScrollableListView Instance => _service;

    public static (int TopRow, int EndRow) ComputeViewport(int totalCount, int selectedIndex, int maxVisibleRows) => _service.ComputeViewport(totalCount, selectedIndex, maxVisibleRows);
    public static int GetPageStep(int maxVisibleRows) => _service.GetPageStep(maxVisibleRows);
    public static string RenderAboveIndicator(int topRow) => _service.RenderAboveIndicator(topRow);
    public static string RenderBelowIndicator(int endRow, int totalCount) => _service.RenderBelowIndicator(endRow, totalCount);
}

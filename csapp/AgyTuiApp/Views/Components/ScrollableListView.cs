using System;
using System.Collections.Generic;
using System.Linq;
using Spectre.Console;

namespace AgyTui;

public static class ScrollableListView
{
    public static (int TopRow, int EndRow) ComputeViewport(int totalCount, int selectedIndex, int maxVisibleRows)
    {
        if (totalCount <= 0) return (0, 0);
        if (maxVisibleRows <= 0) maxVisibleRows = 10;

        int topRow = 0;
        if (selectedIndex >= topRow + maxVisibleRows)
        {
            topRow = selectedIndex - maxVisibleRows + 1;
        }
        if (selectedIndex < topRow)
        {
            topRow = selectedIndex;
        }

        topRow = Math.Max(0, Math.Min(topRow, Math.Max(0, totalCount - maxVisibleRows)));
        int endRow = Math.Min(totalCount, topRow + maxVisibleRows);
        return (topRow, endRow);
    }

    public static int GetPageStep(int maxVisibleRows)
    {
        return Math.Max(1, maxVisibleRows);
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

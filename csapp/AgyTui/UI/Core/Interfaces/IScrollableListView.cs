namespace AgyTui.UI.Core.Interfaces;

public interface IScrollableListView
{
    (int TopRow, int EndRow) ComputeViewport(int totalCount, int selectedIndex, int maxVisibleRows);
    int GetPageStep(int maxVisibleRows);
    string RenderAboveIndicator(int topRow);
    string RenderBelowIndicator(int endRow, int totalCount);
}

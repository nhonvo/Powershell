namespace AgyTui.Tests.Unit.UI.Layouts;

public class MenuRendererBaseTests
{
    [Fact]
    public void ThreePaneRenderer_LongList_ClampsSelectionToVisibleWindowViaComputeViewport()
    {
        var (start, end) = MenuRendererBase.ComputeViewport(50, 25, 10);

        Assert.True(start <= 25);
        Assert.True(25 < end);
        Assert.Equal(10, end - start);
    }

    [Fact]
    public void ComputeViewport_ClampsNearBeginning()
    {
        var (start, end) = MenuRendererBase.ComputeViewport(50, 2, 10);
        Assert.Equal(0, start);
        Assert.Equal(10, end);
    }

    [Fact]
    public void ComputeViewport_ClampsNearEnd()
    {
        var (start, end) = MenuRendererBase.ComputeViewport(50, 48, 10);
        Assert.Equal(40, start);
        Assert.Equal(50, end);
    }
}

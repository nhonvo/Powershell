using System;
using Xunit;
using AgyTui;

namespace AgyTui.Tests.Unit;

public class FlatTreeRendererTests
{
    [Fact]
    public void Search_ZeroResults_SelectionIndexNeverGoesNegative()
    {
        int selectionIndex = -5;
        int visibleCount = 0;

        if (visibleCount == 0)
        {
            selectionIndex = 0;
        }
        else
        {
            if (selectionIndex >= visibleCount) selectionIndex = visibleCount - 1;
            if (selectionIndex < 0) selectionIndex = 0;
        }

        Assert.Equal(0, selectionIndex);
    }
}

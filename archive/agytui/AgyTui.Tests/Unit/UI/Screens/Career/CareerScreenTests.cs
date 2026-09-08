using AgyTui.UI.Screens.Career;
using AgyTui.UI.Screens.Career.Helpers;

namespace AgyTui.Tests.Unit.UI.Screens.Career;

public class CareerScreenTests
{
    [Fact]
    public void InterviewBank_StaticType_Exists()
    {
        Assert.NotNull(typeof(InterviewBank));
    }

    [Fact]
    public void AlgoVisualizer_StaticType_Exists()
    {
        Assert.NotNull(typeof(AlgoVisualizer));
    }
}

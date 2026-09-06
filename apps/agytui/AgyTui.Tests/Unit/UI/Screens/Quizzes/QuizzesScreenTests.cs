using AgyTui.UI.Screens.Quizzes;
using AgyTui.UI.Screens.Quizzes.Helpers;

namespace AgyTui.Tests.Unit.UI.Screens.Quizzes;

public class QuizzesScreenTests
{
    [Fact]
    public void CsharpQuiz_StaticType_Exists()
    {
        Assert.NotNull(typeof(CsharpQuiz));
    }

    [Fact]
    public void KanaQuiz_StaticType_Exists()
    {
        Assert.NotNull(typeof(KanaQuiz));
    }
}

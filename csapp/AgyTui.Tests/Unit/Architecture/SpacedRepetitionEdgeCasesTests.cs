namespace AgyTui.Tests.Unit.Architecture;

public class SpacedRepetitionEdgeCasesTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void UpdateCard_AllQualities_CalculatesCorrectEaseFactorAndPassingStatus(int quality)
    {
        var card = SpacedRepetitionEngine.NewCard();
        var result = SpacedRepetitionEngine.UpdateCard(card, quality);

        double expectedEf = Math.Max(1.3, 2.5 + (0.1 - (5 - quality) * (0.08 + (5 - quality) * 0.02)));
        Assert.Equal(expectedEf, result.Updated.EaseFactor, 4);

        bool expectedPassed = quality >= 3;
        Assert.Equal(expectedPassed, result.Passed);
        if (!expectedPassed)
        {
            Assert.Equal(0, result.Updated.Repetitions);
            Assert.Equal(1, result.NextIntervalDays);
            Assert.Equal("learning", result.Updated.Status);
        }
        else
        {
            Assert.Equal(1, result.Updated.Repetitions);
            Assert.Equal(1, result.NextIntervalDays);
        }
    }

    [Fact]
    public void UpdateCard_BrandNewCard_Repetitions0_UpdatesToRepetitions1()
    {
        var card = SpacedRepetitionEngine.NewCard();
        Assert.Equal(0, card.Repetitions);
        Assert.Null(card.LastReviewed);

        var result = SpacedRepetitionEngine.UpdateCard(card, 5);
        Assert.Equal(1, result.Updated.Repetitions);
        Assert.NotNull(result.Updated.LastReviewed);
    }

    [Fact]
    public void UpdateCard_LongStreakThenFail_ResetsIntervalTo1AndRepetitionsTo0()
    {
        var card = new SrState(2.5, 30, 10, DateTime.Now.AddDays(-30), DateTime.Today, "mastered");
        var result = SpacedRepetitionEngine.UpdateCard(card, 0);

        Assert.False(result.Passed);
        Assert.Equal(0, result.Updated.Repetitions);
        Assert.Equal(1, result.NextIntervalDays);
        Assert.Equal("learning", result.Updated.Status);
    }

    [Fact]
    public void IsDueToday_NullOrPastOrFutureDates_EvaluatedCorrectly()
    {
        var newCard = SpacedRepetitionEngine.NewCard();
        Assert.True(SpacedRepetitionEngine.IsDueToday(newCard));

        var pastCard = new SrState(2.5, 5, 2, DateTime.Now.AddDays(-10), DateTime.Today.AddDays(-2), "review");
        Assert.True(SpacedRepetitionEngine.IsDueToday(pastCard));

        var futureCard = new SrState(2.5, 5, 2, DateTime.Now, DateTime.Today.AddDays(5), "review");
        Assert.False(SpacedRepetitionEngine.IsDueToday(futureCard));
    }
}

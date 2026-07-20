using System;
using AgyTui;
using Xunit;

namespace AgyTuiApp.Tests;

public class SpacedRepetitionTests
{
    [Fact]
    public void UpdateCard_QualityZero_ResetsIntervalAndRepetitions()
    {
        var state = new SrState(2.5, 10, 3, DateTime.UtcNow.AddDays(-10), DateTime.UtcNow, "review");
        var result = SpacedRepetitionEngine.UpdateCard(state, quality: 0);

        Assert.False(result.Passed);
        Assert.Equal(0, result.Updated.Repetitions);
        Assert.Equal(1, result.Updated.IntervalDays);
        Assert.True(result.Updated.EaseFactor < 2.5);
    }

    [Fact]
    public void UpdateCard_QualityFive_IncreasesIntervalAndEaseFactor()
    {
        var state = new SrState(2.5, 1, 1, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow, "review");
        var result = SpacedRepetitionEngine.UpdateCard(state, quality: 5);

        Assert.True(result.Passed);
        Assert.Equal(2, result.Updated.Repetitions);
        Assert.Equal(6, result.Updated.IntervalDays);
        Assert.True(result.Updated.EaseFactor >= 2.5);
    }

    [Fact]
    public void UpdateCard_EaseFactor_NeverClampsBelowMinimum()
    {
        var state = new SrState(1.3, 1, 1, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow, "review");
        var result = SpacedRepetitionEngine.UpdateCard(state, quality: 0);

        Assert.True(result.Updated.EaseFactor >= 1.3);
    }
}

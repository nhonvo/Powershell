using AgyTui.Domain.LearnContext;

namespace AgyTui.UI.Screens.Learn;

public static class SrStateExtensions
{
    public static SrState NewCard() => new(2.5, 0, 0, null, null, "new");

    public static bool IsDueToday(this SrState state) => state.NextReview == null || state.NextReview.Value.Date <= DateTime.Today;

    public static SrResult UpdateCard(this SrState state, int quality)
    {
        bool passed = quality >= 3;
        double ef = Math.Max(1.3, state.EaseFactor + (0.1 - (5 - quality) * (0.08 + (5 - quality) * 0.02)));
        int reps = passed ? state.Repetitions + 1 : 0;
        int interval = reps switch
        {
            0 => 1,
            1 => 1,
            2 => 6,
            _ => (int)Math.Round(state.IntervalDays * ef)
        };
        if (!passed)
        {
            interval = 1;
        }
        string status = !passed ? "learning" : interval > 21 ? "mastered" : "review";
        var updated = new SrState(ef, interval, reps, DateTime.Now, DateTime.Today.AddDays(interval), status);
        return new SrResult(updated, passed, interval);
    }
}

public sealed record SrResult(SrState Updated, bool Passed, int NextIntervalDays);

public static class SpacedRepetitionEngine
{
    public static SrState NewCard() => SrStateExtensions.NewCard();

    public static bool IsDueToday(SrState sr) => sr.IsDueToday();

    public static SrResult UpdateCard(SrState current, int quality) => current.UpdateCard(quality);

    public static int CardsRemaining(SrState[] states) => states.Count(IsDueToday);
}

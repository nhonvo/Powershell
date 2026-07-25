namespace AgyTui.UI.Screens.Learn;

public sealed record SrState(double EaseFactor, int IntervalDays, int Repetitions, DateTime? LastReviewed, DateTime? NextReview, string Status)
{
    public static SrState NewCard() => new(2.5, 0, 0, null, null, "new");

    public bool IsDueToday() => NextReview == null || NextReview.Value.Date <= DateTime.Today;

    public SrResult UpdateCard(int quality)
    {
        bool passed = quality >= 3;
        double ef = Math.Max(1.3, EaseFactor + (0.1 - (5 - quality) * (0.08 + (5 - quality) * 0.02)));
        int reps = passed ? Repetitions + 1 : 0;
        int interval = reps switch
        {
            0 => 1,
            1 => 1,
            2 => 6,
            _ => (int)Math.Round(IntervalDays * ef)
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
    public static SrState NewCard() => SrState.NewCard();

    public static bool IsDueToday(SrState sr) => sr.IsDueToday();

    public static SrResult UpdateCard(SrState current, int quality) => current.UpdateCard(quality);

    public static int CardsRemaining(SrState[] states) => states.Count(IsDueToday);
}

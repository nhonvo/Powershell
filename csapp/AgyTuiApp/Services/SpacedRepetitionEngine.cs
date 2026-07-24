using System;
using System.Linq;

namespace AgyTui;

public sealed record SrState(double EaseFactor, int IntervalDays, int Repetitions, DateTime? LastReviewed, DateTime? NextReview, string Status);

public sealed record SrResult(SrState Updated, bool Passed, int NextIntervalDays);

public static class SpacedRepetitionEngine
{
    public static SrState NewCard() => new(2.5, 0, 0, null, null, "new");

    public static bool IsDueToday(SrState sr) => sr.NextReview == null || sr.NextReview.Value.Date <= DateTime.Today;

    public static SrResult UpdateCard(SrState current, int quality)
    {
        bool passed = quality >= 3;
        double ef = Math.Max(1.3, current.EaseFactor + (0.1 - (5 - quality) * (0.08 + (5 - quality) * 0.02)));
        int reps = passed ? current.Repetitions + 1 : 0;
        int interval = reps switch
        {
            0 => 1,
            1 => 1,
            2 => 6,
            _ => (int)Math.Round(current.IntervalDays * ef)
        };
        if (!passed)
        {
            interval = 1;
        }
        string status = !passed ? "learning" : interval > 21 ? "mastered" : "review";
        var updated = new SrState(ef, interval, reps, DateTime.Now, DateTime.Today.AddDays(interval), status);
        return new SrResult(updated, passed, interval);
    }

    public static int CardsRemaining(SrState[] states) => states.Count(IsDueToday);
}

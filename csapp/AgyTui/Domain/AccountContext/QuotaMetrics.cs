namespace AgyTui.Domain.AccountContext;

public sealed record QuotaMetrics(
    double RemainingWeekly,
    double Remaining5H,
    string TimeWeekly,
    string Time5H,
    int CountWeekly,
    int Count5H,
    string ExhaustionWeekly,
    string Exhaustion5H);

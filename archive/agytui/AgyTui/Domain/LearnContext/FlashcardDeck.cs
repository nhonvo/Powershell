namespace AgyTui.Domain.LearnContext;

public class FlashcardDeck
{
    public string Topic { get; private set; }
    public int CardsCount { get; private set; }
    public double AverageEaseFactor { get; private set; }
    public DateTime LastReviewedUtc { get; private set; }

    public FlashcardDeck(string topic, int cardsCount = 0, double averageEaseFactor = 2.5, DateTime? lastReviewedUtc = null)
    {
        Topic = topic;
        CardsCount = cardsCount;
        AverageEaseFactor = averageEaseFactor;
        LastReviewedUtc = lastReviewedUtc ?? DateTime.UtcNow;
    }

    public void UpdateStats(int cardsCount, double averageEaseFactor)
    {
        CardsCount = cardsCount;
        AverageEaseFactor = averageEaseFactor;
        LastReviewedUtc = DateTime.UtcNow;
    }
}

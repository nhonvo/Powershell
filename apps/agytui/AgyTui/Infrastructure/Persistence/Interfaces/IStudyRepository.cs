namespace AgyTui.Infrastructure.Persistence.Interfaces;

public interface IStudyRepository
{
    void EnsureDirectories();
    T? LoadJson<T>(string path) where T : class;
    bool SaveJson<T>(string path, T obj);
    FlashcardDeck LoadDeck(string topic);
    bool SaveDeck(FlashcardDeck deck);
}

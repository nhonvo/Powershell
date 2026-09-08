namespace AgyTui.Infrastructure.Integrations.Obsidian;

public interface IObsidianBridge
{
    void Configure();
    void Run();
    void SearchNotes();
    void ShowDailyNote(string? vaultPath = null);
    void ListByTag();
}

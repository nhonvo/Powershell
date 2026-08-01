namespace AgyTui.UI.Screens.Interfaces;

public interface ILearnSuite
{
    void RunLearnRouter();
    void RunFlashcards();
    void RunStudySession(string topic = "General", int durationMinutes = 25, int breakMinutes = 5);
}

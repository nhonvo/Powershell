using AgyTui.UI.Screens.Career;
using AgyTui.UI.Screens.GitNexus;
using AgyTui.UI.Screens.Ide;
using AgyTui.UI.Screens.Services;
using AgyTui.UI.Screens.Learn;

namespace AgyTui.UI.Screens.Services;

public class CareerSuiteService : ICareerSuite
{
    public void RunAlgoVisualizer() => AlgoVisualizer.PickAndRun();
    public void RunInterviewBank() => InterviewBank.Run();
}

public class GitNexusSuiteService : IGitNexusSuite
{
    public void RunGitNexus() => GitNexusStats.Run();
}

public class IdeSuiteService : IIdeSuite
{
    public void RunTerminalIde(string? initialPath = null) => TerminalIde.Open(initialPath);
    public void RunDiffViewer(string workspacePath, string? filePath = null) => GitDiffViewer.ShowDiff(workspacePath, filePath);
    public void RunCodeViewer(string filePath) => CodeViewer.Show(filePath);
    public void RunSymbolSearch(string dirPath) => SymbolSearch.BrowseWorkspaceSymbols(dirPath);
}

public class LearnSuiteService : ILearnSuite
{
    public void RunLearnRouter() => LearnRouter.LaunchMasterHub();
    public void RunFlashcards() => FlashcardEngine.PickAndRun("decks");
    public void RunStudySession(string topic = "General", int durationMinutes = 25, int breakMinutes = 5) => StudySession.Run(topic, durationMinutes, breakMinutes);
}


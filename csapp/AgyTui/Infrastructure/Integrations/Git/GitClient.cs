using AgyTui.Infrastructure.Integrations.Ai.Abstractions;

namespace AgyTui.Infrastructure.Integrations.Git;

public class GitClient : CliToolWrapper, IGitClient
{
    private static readonly string[] CommitTypes = ["feat", "fix", "docs", "style", "refactor", "test", "chore", "ci"];
    private readonly IAiCommitGenerator _commitGenerator;

    public GitClient(IAiCommitGenerator commitGenerator) : base("git")
    {
        _commitGenerator = commitGenerator;
    }

    public void ShowStatus()
    {
        var branch = RunGit("branch --show-current").Trim();
        if (string.IsNullOrEmpty(branch)) branch = "main";

        AnsiConsole.Write(new Rule($"[bold cyan]Git Status ({branch.EscapeMarkup()})[/]").RuleStyle("grey"));
        var output = RunGit("status --short");
        if (string.IsNullOrWhiteSpace(output))
        {
            SpectrePanel.Success("Working tree clean. No changes detected.");
            return;
        }

        var table = new Table().Border(TableBorder.Rounded).BorderColor(Color.Grey);
        table.AddColumn("State");
        table.AddColumn("File Path");

        foreach (var line in output.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)))
        {
            var trimmed = line.TrimEnd('\r');
            if (trimmed.Length < 3) continue;
            var code = trimmed[..2];
            var file = trimmed[3..].Trim();

            var (label, color) = code switch
            {
                "??" => ("Untracked", "dim"),
                " M" or "M " or "MM" => ("Modified", "yellow"),
                " A" or "A " => ("Staged", "green"),
                " D" or "D " => ("Deleted", "red"),
                " R" or "R " => ("Renamed", "cyan"),
                _ => ("Changed", "white")
            };

            table.AddRow($"[{color}]{label} ({code.Trim()})[/]", $"[bold white]{file.EscapeMarkup()}[/]");
        }

        AnsiConsole.Write(table);
    }

    public void ShowStatusNative(string[]? passArgs = null)
    {
        var extra = passArgs != null && passArgs.Length > 0 ? string.Join(" ", passArgs) : "";
        RunGitDirect($"status {extra}".Trim());
    }

    public void ConventionalCommitWizard()
    {
        AnsiConsole.Write(new Rule("[bold cyan]Conventional Commit Wizard[/]").RuleStyle("grey"));
        var typeIdx = SpectreMenu.Show("Commit Type", CommitTypes, 0, false);
        if (typeIdx < 0) return;
        var commitType = CommitTypes[typeIdx];
        var scope = AnsiConsole.Ask("[dim]Scope[/] (optional, press Enter to skip):", string.Empty).Trim();
        var scopePart = string.IsNullOrWhiteSpace(scope) ? string.Empty : $"({scope})";

        string description;
        var draftWithAi = AnsiConsole.Confirm("Would you like local AI to draft the description from staged diff?");
        string draft = "";
        if (draftWithAi)
        {
            var diff = RunGit("diff --cached").Trim();
            if (string.IsNullOrEmpty(diff))
            {
                SpectrePanel.Warning("No staged diff found. Please stage changes first.");
            }
            else
            {
                AnsiConsole.MarkupLine("[cyan]Querying local AI to draft description...[/]");
                draft = _commitGenerator.GenerateDraftDescription(diff);
                if (!string.IsNullOrEmpty(draft))
                {
                    AnsiConsole.MarkupLine($"[green]Suggested draft:[/] {draft}");
                }
            }
        }

        while (true)
        {
            description = string.IsNullOrEmpty(draft)
                ? AnsiConsole.Ask<string>("[cyan]Short description[/]:").Trim()
                : AnsiConsole.Ask<string>("[cyan]Short description[/]:", draft).Trim();
            if (description.Length is >= 5 and <= 72) break;
            SpectrePanel.Warning("Description must be 5–72 characters.");
        }

        var breaking = AnsiConsole.Ask<string>("[dim]Breaking changes[/] (optional):", string.Empty).Trim();
        var issues = AnsiConsole.Ask<string>("[dim]Issues closed[/] (e.g. #42, optional):", string.Empty).Trim();
        var sb = new StringBuilder($"{commitType}{scopePart}: {description}");
        if (!string.IsNullOrWhiteSpace(breaking)) sb.Append($"\n\nBREAKING CHANGE: {breaking}");
        if (!string.IsNullOrWhiteSpace(issues)) sb.Append($"\n\nCloses {issues}");
        var message = sb.ToString();
        AnsiConsole.Write(new Panel(message.EscapeMarkup())
        {
            Header = new PanelHeader("[bold]Commit Message Preview[/]"),
            Border = BoxBorder.Rounded
        });
        if (!AnsiConsole.Confirm("Commit now?")) return;
        var exitCode = Helpers.ProcessRunner.Instance.Run(BinaryName, ["commit", "-m", message]);
        if (exitCode == 0) SpectrePanel.Success("Committed successfully.");
        else SpectrePanel.Error($"git commit failed (exit {exitCode}).");
    }

    public void InvokeGitUndo()
    {
        var lastLog = RunGit("log --oneline -1").Trim();
        AnsiConsole.MarkupLine($"[yellow]Last commit:[/] {lastLog.EscapeMarkup()}");
        try
        {
            if (!AnsiConsole.Confirm("Soft-reset (keep changes staged)?")) return;
        }
        catch (InvalidOperationException)
        {
            SpectrePanel.Warning("Non-interactive terminal detected. Skipping confirmation.");
            return;
        }
        var exit = RunGitDirect("reset HEAD~1 --soft");
        if (exit == 0) SpectrePanel.Success("Last commit undone. Changes kept in working directory.");
        else SpectrePanel.Error($"git reset failed (exit {exit}).");
    }

    public void Checkout(string? branchName = null)
    {
        if (!string.IsNullOrWhiteSpace(branchName))
        {
            AnsiConsole.MarkupLine($"[cyan]Checking out branch:[/] [bold green]{branchName.EscapeMarkup()}[/]");
            var exitCode = RunGitDirect($"checkout \"{branchName}\"");
            if (exitCode == 0) SpectrePanel.Success($"Checked out '{branchName}'.");
            else SpectrePanel.Error($"git checkout failed (exit {exitCode}).");
            return;
        }
        ShowBranches();
    }

    public void ShowBranches()
    {
        var output = RunGit("branch -a --sort=-committerdate");
        if (string.IsNullOrWhiteSpace(output))
        {
            SpectrePanel.Warning("Not a git repository or no branches found.");
            return;
        }
        var branches = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        AnsiConsole.Write(new Rule("[bold cyan]Git Branches (Sorted by recent activity)[/]").RuleStyle("grey"));
        var selectedIdx = SpectreMenu.Show("Select Branch to Checkout", branches, 0, false);
        if (selectedIdx < 0) return;

        var targetBranch = branches[selectedIdx].TrimStart('*', ' ').Trim();
        if (targetBranch.StartsWith("remotes/"))
        {
            var parts = targetBranch.Split('/', 3);
            if (parts.Length == 3) targetBranch = parts[2];
        }

        AnsiConsole.MarkupLine($"[cyan]Checking out branch:[/] [bold green]{targetBranch.EscapeMarkup()}[/]");
        var exitCode = RunGitDirect($"checkout \"{targetBranch}\"");
        if (exitCode == 0) SpectrePanel.Success($"Checked out '{targetBranch}'.");
        else SpectrePanel.Error($"git checkout failed (exit {exitCode}).");
    }

    public void ShowLog()
    {
        var output = RunGit("log --oneline --graph --decorate -n 50");
        if (string.IsNullOrWhiteSpace(output))
        {
            SpectrePanel.Info("No commit history found.");
            return;
        }
        SpectrePager.Show("Git Commit Log (Last 50)", output);
    }

    public void Pull()
    {
        AnsiConsole.MarkupLine("[cyan]Pulling latest changes from remote...[/]");
        var exitCode = RunGitDirect("pull");
        if (exitCode == 0) SpectrePanel.Success("Git pull completed successfully.");
        else SpectrePanel.Error($"git pull failed (exit {exitCode}).");
    }

    public void Push()
    {
        AnsiConsole.MarkupLine("[cyan]Pushing local commits to remote...[/]");
        var exitCode = RunGitDirect("push");
        if (exitCode == 0) SpectrePanel.Success("Git push completed successfully.");
        else SpectrePanel.Error($"git push failed (exit {exitCode}).");
    }

    public void AddAll()
    {
        AnsiConsole.MarkupLine("[cyan]Staging all modified and new files...[/]");
        var exitCode = RunGitDirect("add .");
        if (exitCode == 0) SpectrePanel.Success("Staged all workspace changes.");
        else SpectrePanel.Error($"git add failed (exit {exitCode}).");
    }

    public void Fetch()
    {
        AnsiConsole.MarkupLine("[cyan]Fetching remote references...[/]");
        var exitCode = RunGitDirect("fetch");
        if (exitCode == 0) SpectrePanel.Success("Fetched latest remote references.");
        else SpectrePanel.Error($"git fetch failed (exit {exitCode}).");
    }

    private string RunGit(string args) => RunCapture(args);

    private int RunGitDirect(string args)
    {
        return Helpers.ProcessRunner.Instance.Run(BinaryName, args);
    }

    public void ShowDiff() => RunGitDirect("diff");
    public void ShowLogGraph() => RunGitDirect("log --graph --oneline --decorate --all");
    public void ShowLogPretty() => RunGitDirect("log --pretty=format:\"%h - %an, %ar : %s\"");
    public void NewBranch(string? branchName = null)
    {
        if (string.IsNullOrWhiteSpace(branchName)) branchName = AnsiConsole.Ask<string>("New branch name:");
        RunGitDirect($"checkout -b \"{branchName}\"");
    }
    public void RemoveBranch(string? branchName = null)
    {
        if (string.IsNullOrWhiteSpace(branchName)) branchName = AnsiConsole.Ask<string>("Branch to delete:");
        RunGitDirect($"branch -d \"{branchName}\"");
    }
    public void UnstageAll() => RunGitDirect("restore --staged .");
    public void CommitAmend(string[]? passArgs = null)
    {
        var extra = passArgs != null && passArgs.Length > 0 ? string.Join(" ", passArgs) : "";
        RunGitDirect($"commit --amend {extra}".Trim());
    }
    public void ResetSoft() => RunGitDirect("reset --soft HEAD~1");
    public void ResetHard() => RunGitDirect("reset --hard");
    public void PushForce(string[]? passArgs = null)
    {
        var extra = passArgs != null && passArgs.Length > 0 ? string.Join(" ", passArgs) : "";
        RunGitDirect($"push --force {extra}".Trim());
    }
    public void CloneProject(string? url = null, string? destName = null)
    {
        if (string.IsNullOrWhiteSpace(url)) url = AnsiConsole.Ask<string>("Git clone repository URL:");
        var baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Documents");
        if (string.IsNullOrWhiteSpace(destName))
        {
            var match = System.Text.RegularExpressions.Regex.Match(url, @"/([^/]+?)(\.git)?$");
            destName = match.Success ? match.Groups[1].Value : "cloned-project-" + Random.Shared.Next(1000, 9999);
        }
        var targetPath = Path.Combine(baseDir, destName);
        SpectrePanel.Info($"Cloning project from {url} into {targetPath}...");
        var exitCode = RunGitDirect($"clone \"{url}\" \"{targetPath}\"");
        if (exitCode == 0) SpectrePanel.Success($"Project successfully cloned into {targetPath}!");
        else SpectrePanel.Error("Failed to clone repository.");
    }
}

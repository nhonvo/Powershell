using AgyTui.Infrastructure.Integrations.Ai.Abstractions;

namespace AgyTui.Infrastructure.Integrations.Git;

public class GitClient : CliToolWrapper, IGitClient
{
    private static readonly string[] CommitTypes = ["feat", "fix", "docs", "style", "refactor", "test", "chore", "ci"];
    private readonly IAiCommitGenerator _commitGenerator;

    public GitClient(IAiCommitGenerator? commitGenerator = null) : base("git")
    {
        _commitGenerator = commitGenerator!;
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

    public void ShowRemotesNative(string[]? passArgs = null)
    {
        var extra = passArgs != null && passArgs.Length > 0 ? string.Join(" ", passArgs) : "";
        RunGitDirect($"remote -v {extra}".Trim());
    }

    public void ShowRemotes()
    {
        var output = RunGit("remote -v");
        if (string.IsNullOrWhiteSpace(output))
        {
            SpectrePanel.Warning("No remote repositories configured.");
            return;
        }

        AnsiConsole.Write(new Rule("[bold cyan]Git Remotes[/]").RuleStyle("grey"));
        var table = new Table().Border(TableBorder.Rounded).BorderColor(Color.Grey);
        table.AddColumn("Remote Name");
        table.AddColumn("URL");
        table.AddColumn("Type");

        foreach (var line in output.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)))
        {
            var parts = line.Split(['\t', ' '], StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 3)
            {
                table.AddRow($"[bold cyan]{parts[0].EscapeMarkup()}[/]", $"[white]{parts[1].EscapeMarkup()}[/]", $"[dim]{parts[2].Trim('(', ')').EscapeMarkup()}[/]");
            }
        }

        AnsiConsole.Write(table);

        var actions = new[]
        {
            "🌿 Fetch All Remotes (git fetch --all)",
            "🌿 Checkout Remote Branch (--track)",
            "➕ Add New Remote (git remote add)",
            "↩ Back"
        };

        var choice = SpectreMenu.Show("Remote Actions", actions, 0);
        switch (choice)
        {
            case 0:
                RunGitDirect("fetch --all");
                break;
            case 1:
                CheckoutRemoteBranch();
                break;
            case 2:
                var rName = AnsiConsole.Ask<string>("Remote Name (e.g. upstream):").Trim();
                var rUrl = AnsiConsole.Ask<string>("Remote URL:").Trim();
                if (!string.IsNullOrEmpty(rName) && !string.IsNullOrEmpty(rUrl))
                {
                    RunGitDirect($"remote add \"{rName}\" \"{rUrl}\"");
                }
                break;
        }
    }

    public void CheckoutRemoteBranch(string? remoteBranch = null)
    {
        if (string.IsNullOrWhiteSpace(remoteBranch))
        {
            RunGitDirect("fetch --all");
            var output = RunGit("branch -r");
            if (string.IsNullOrWhiteSpace(output))
            {
                SpectrePanel.Warning("No remote branches found.");
                return;
            }
            var branches = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                 .Where(b => !b.Contains("->"))
                                 .ToArray();
            var selectedIdx = SpectreMenu.Show("Select Remote Branch to Checkout", branches, 0, false);
            if (selectedIdx < 0) return;
            remoteBranch = branches[selectedIdx];
        }

        remoteBranch = remoteBranch.Trim();
        AnsiConsole.MarkupLine($"[cyan]Checking out remote branch:[/] [bold green]{remoteBranch.EscapeMarkup()}[/]");
        var exitCode = RunGitDirect($"checkout --track \"{remoteBranch}\"");
        if (exitCode != 0)
        {
            var localName = remoteBranch.Contains('/') ? remoteBranch[(remoteBranch.IndexOf('/') + 1)..] : remoteBranch;
            exitCode = RunGitDirect($"checkout -b \"{localName}\" \"{remoteBranch}\"");
        }

        if (exitCode == 0) SpectrePanel.Success($"Successfully checked out remote branch '{remoteBranch}'!");
        else SpectrePanel.Error($"Failed to checkout remote branch '{remoteBranch}'.");
    }

    public void MergeBranch(string? branchName = null)
    {
        if (string.IsNullOrWhiteSpace(branchName))
        {
            ShowMergeWizard();
            return;
        }
        AnsiConsole.MarkupLine($"[cyan]Merging branch:[/] [bold yellow]{branchName.EscapeMarkup()}[/]");
        var exitCode = RunGitDirect($"merge \"{branchName}\"");
        if (exitCode == 0) SpectrePanel.Success($"Merged '{branchName}' successfully.");
        else
        {
            SpectrePanel.Error($"Merge conflict or failure detected (exit {exitCode}).");
            AnsiConsole.MarkupLine("[yellow]Run [bold]gconflictu[/] to resolve unmerged files.[/]");
        }
    }

    public void ShowMergeWizard()
    {
        var output = RunGit("branch -a");
        if (string.IsNullOrWhiteSpace(output))
        {
            SpectrePanel.Warning("No branches found to merge.");
            return;
        }

        var branches = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                             .Where(b => !b.StartsWith("*"))
                             .Select(b => b.Trim())
                             .ToArray();

        if (branches.Length == 0)
        {
            SpectrePanel.Info("No other branches available for merging.");
            return;
        }

        AnsiConsole.Write(new Rule("[bold cyan]Git Merge Wizard[/]").RuleStyle("grey"));
        var selectedIdx = SpectreMenu.Show("Select Branch to Merge into Current HEAD", branches, 0, false);
        if (selectedIdx < 0) return;

        var target = branches[selectedIdx];
        MergeBranch(target);
    }

    public void ShowConflictResolver()
    {
        var output = RunGit("diff --name-only --diff-filter=U");
        var conflictedFiles = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (conflictedFiles.Length == 0)
        {
            SpectrePanel.Success("No active merge conflicts detected in working tree!");
            return;
        }

        AnsiConsole.Write(new Rule("[bold red]✨ Git Conflict Resolution Helper[/]").RuleStyle("red"));
        AnsiConsole.MarkupLine($"[bold yellow]Found {conflictedFiles.Length} file(s) with unmerged conflicts:[/]");

        var table = new Table().Border(TableBorder.Rounded).BorderColor(Color.Red);
        table.AddColumn("Conflicted File");
        foreach (var f in conflictedFiles) table.AddRow($"[bold red]{f.EscapeMarkup()}[/]");
        AnsiConsole.Write(table);

        var actions = new[]
        {
            "🔍 Inspect Ours vs Theirs Diffs",
            "🛡 Accept Ours (git checkout --ours .)",
            "🚀 Accept Theirs (git checkout --theirs .)",
            "✅ Stage Resolved Files (git add .)",
            "↩ Cancel / Back"
        };

        var choice = SpectreMenu.Show("Conflict Resolution Actions", actions, 0);
        switch (choice)
        {
            case 0:
                RunGitDirect("diff --cc");
                break;
            case 1:
                if (AnsiConsole.Confirm("Overwrites conflicted files with current branch version (ours)?"))
                {
                    RunGitDirect("checkout --ours .");
                    RunGitDirect("add .");
                    SpectrePanel.Success("Applied 'ours' changes to all conflicted files.");
                }
                break;
            case 2:
                if (AnsiConsole.Confirm("Overwrites conflicted files with incoming branch version (theirs)?"))
                {
                    RunGitDirect("checkout --theirs .");
                    RunGitDirect("add .");
                    SpectrePanel.Success("Applied 'theirs' changes to all conflicted files.");
                }
                break;
            case 3:
                RunGitDirect("add .");
                SpectrePanel.Success("Staged all resolved conflict files.");
                break;
        }
    }

    public void ShowStashManager()
    {
        var output = RunGit("stash list");
        var stashes = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        AnsiConsole.Write(new Rule("[bold cyan]✨ Git Stash Manager[/]").RuleStyle("grey"));

        if (stashes.Length == 0)
        {
            AnsiConsole.MarkupLine("[dim]No stashes saved.[/]");
            if (AnsiConsole.Confirm("Create a new stash now?"))
            {
                var msg = AnsiConsole.Ask<string>("Stash message (optional):", string.Empty);
                var args = string.IsNullOrWhiteSpace(msg) ? "stash" : $"stash push -m \"{msg}\"";
                RunGitDirect(args);
            }
            return;
        }

        var table = new Table().Border(TableBorder.Rounded).BorderColor(Color.Grey);
        table.AddColumn("Stash Identifier");
        table.AddColumn("Description");
        foreach (var s in stashes)
        {
            var parts = s.Split(':', 2);
            table.AddRow($"[bold cyan]{parts[0].EscapeMarkup()}[/]", parts.Length > 1 ? parts[1].EscapeMarkup() : "");
        }
        AnsiConsole.Write(table);

        var actions = new[]
        {
            "💾 Save New Stash (git stash push)",
            "📦 Pop Latest Stash (git stash pop)",
            "⚡ Apply Latest Stash (git stash apply)",
            "🗑 Clear All Stashes (git stash clear)",
            "↩ Back"
        };

        var choice = SpectreMenu.Show("Stash Actions", actions, 0);
        switch (choice)
        {
            case 0:
                var msg = AnsiConsole.Ask<string>("Stash message (optional):", string.Empty);
                var args = string.IsNullOrWhiteSpace(msg) ? "stash" : $"stash push -m \"{msg}\"";
                RunGitDirect(args);
                break;
            case 1:
                RunGitDirect("stash pop");
                break;
            case 2:
                RunGitDirect("stash apply");
                break;
            case 3:
                if (AnsiConsole.Confirm("Delete all stashes permanently?")) RunGitDirect("stash clear");
                break;
        }
    }

    public void ShowRebaseWizard(string? branchName = null)
    {
        if (string.IsNullOrWhiteSpace(branchName))
        {
            var output = RunGit("branch -a");
            var branches = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                 .Where(b => !b.StartsWith("*"))
                                 .Select(b => b.Trim())
                                 .ToArray();

            if (branches.Length == 0)
            {
                SpectrePanel.Warning("No target branches available for rebase.");
                return;
            }

            AnsiConsole.Write(new Rule("[bold cyan]Git Rebase Wizard[/]").RuleStyle("grey"));
            var idx = SpectreMenu.Show("Select Base Branch for Rebase", branches, 0, false);
            if (idx < 0) return;
            branchName = branches[idx];
        }

        AnsiConsole.MarkupLine($"[cyan]Rebasing current branch onto:[/] [bold green]{branchName.EscapeMarkup()}[/]");
        var exitCode = RunGitDirect($"rebase \"{branchName}\"");
        if (exitCode == 0) SpectrePanel.Success($"Rebased onto '{branchName}' successfully.");
        else SpectrePanel.Error($"Rebase stopped due to conflicts or error (exit {exitCode}). Run 'git rebase --continue' or 'git rebase --abort'.");
    }
}

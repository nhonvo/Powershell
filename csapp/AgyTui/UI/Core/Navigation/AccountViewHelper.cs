namespace AgyTui.UI.Core.Navigation;

public static class AgyAccountMenu
{
    public enum MainMenuChoice
    {
        Exit, ManageAccount, AddAccount, ToggleAutoSwitch, ShowStats
    }

    public sealed record MainMenuResult(MainMenuChoice Choice, string? AccountName);

    public static MainMenuResult ShowManageMenu()
    {
        var accounts = AgyAccountCore.GetAccounts();
        var active = AgyAccountCore.GetActiveAccount();
        var menuItems = new List<string>();
        var defaultIdx = 0;
        for (var i = 0; i < accounts.Length; i++)
        {
            var status = File.Exists(System.IO.Path.Combine(AgyAccountCore.GetAccountDirectory(accounts[i]), "keyring_token.txt")) ? "Logged In" : "Not Logged In";
            if (accounts[i] == active)
            {
                menuItems.Add($"* {accounts[i]} (Active, {status})");
                defaultIdx = i;
            }
            else menuItems.Add($" {accounts[i]} ({status})");
        }
        menuItems.Add("+ Add New Account");
        menuItems.Add($"[Settings] Toggle Auto-Switch (Currently: {(AgyAccountCore.IsAutoSwitchEnabled() ? "Enabled" : "Disabled")})");
        menuItems.Add("[Stats] Show All Accounts Summary");
        menuItems.Add("[x] Exit Dashboard");
        var selected = SpectreMenu.ShowRobust(["Antigravity Multi-Account Manager"], menuItems.ToArray(), defaultIdx, false, true);
        if (selected < 0 || selected == menuItems.Count - 1) return new(MainMenuChoice.Exit, null);
        if (selected < accounts.Length) return new(MainMenuChoice.ManageAccount, accounts[selected]);
        if (selected == accounts.Length) return new(MainMenuChoice.AddAccount, null);
        if (selected == accounts.Length + 1) return new(MainMenuChoice.ToggleAutoSwitch, null);
        return new(MainMenuChoice.ShowStats, null);
    }

    public enum AccountAction
    {
        Back, SetActivePersistent, SetActiveTemporary, ShowUsage, Login, Logout, Delete
    }

    public static void ShowAccountStatsCard(string accountName)
    {
        var stats = AgyAccountCore.GetAccountStats(accountName);
        AnsiConsole.MarkupLine("[cyan]=============================================[/]");
        AnsiConsole.MarkupLine($"[cyan] ACCOUNT STATS: {accountName.EscapeMarkup()}[/]");
        AnsiConsole.MarkupLine("[cyan]=============================================[/]");
        AnsiConsole.MarkupLine($" * Status: {stats.TokenStatus.EscapeMarkup()}");
        AnsiConsole.MarkupLine($" * Quota Status: {stats.QuotaStatus.EscapeMarkup()}");
        AnsiConsole.MarkupLine($" * Last Used: {stats.LastUsed.EscapeMarkup()}");
        AnsiConsole.MarkupLine($" * Usage Count: {stats.UsageCount} sessions/calls");
        AnsiConsole.MarkupLine($" * Private Size: {stats.PrivateSize.EscapeMarkup()} (excluding shared)");
        AnsiConsole.MarkupLine($" * Sync Health: {stats.JunctionStatus.EscapeMarkup()}");
        AnsiConsole.MarkupLine($" * Shared Skills: {stats.SkillsCount} skills");
        AnsiConsole.MarkupLine($" * Shared History: {stats.ConversationsCount} conversations");
        AnsiConsole.MarkupLine("[cyan]=============================================[/]");
        AnsiConsole.WriteLine();
    }

    public static AccountAction ShowAccountSubMenu(string accountName)
    {
        AnsiConsole.Clear();
        ShowAccountStatsCard(accountName);
        var status = File.Exists(System.IO.Path.Combine(AgyAccountCore.GetAccountDirectory(accountName), "keyring_token.txt")) ? "Logged In" : "Not Logged In";
        var subItems = new List<string>
        {
            "[Switch] Set as Active (Persistent)","[Switch] Set as Active (Temporary)","[Usage] Models & Quota","[Login] Sign In / Re-authenticate","[Logout] Sign Out / Reset Credentials"
        };
        if (!string.Equals(accountName, "default", StringComparison.OrdinalIgnoreCase)) subItems.Add("[Delete] Remove Account");
        subItems.Add("[Back] Return to Main Menu");
        var subSel = SpectreMenu.ShowRobust([$"Manage Account: {accountName} ({status})"], subItems.ToArray(), 0, false, true);
        if (subSel < 0) return AccountAction.Back;
        return subItems[subSel] switch
        {
            "[Switch] Set as Active (Persistent)" => AccountAction.SetActivePersistent,
            "[Switch] Set as Active (Temporary)" => AccountAction.SetActiveTemporary,
            "[Usage] Models & Quota" => AccountAction.ShowUsage,
            "[Login] Sign In / Re-authenticate" => AccountAction.Login,
            "[Logout] Sign Out / Reset Credentials" => AccountAction.Logout,
            "[Delete] Remove Account" => AccountAction.Delete,
            _ => AccountAction.Back
        };
    }

    public enum SelectChoice
    {
        Cancel, Selected, AddAccount, DeleteAccount
    }

    public sealed record SelectResult(SelectChoice Choice, string? AccountName);

    public static SelectResult ShowSelectAccountMenu()
    {
        var accounts = AgyAccountCore.GetAccounts();
        var active = AgyAccountCore.GetActiveAccount();
        var menuItems = new List<string>();
        var defaultIdx = 0;
        for (var i = 0; i < accounts.Length; i++)
        {
            if (accounts[i] == active)
            {
                menuItems.Add($"{accounts[i]} (Active)");
                defaultIdx = i;
            }
            else menuItems.Add(accounts[i]);
        }
        menuItems.Add("+ Add New Account");
        menuItems.Add("[x] Delete Account");
        menuItems.Add("[exit] Cancel / Exit");
        var selected = SpectreMenu.ShowRobust(["Select Antigravity Account"], menuItems.ToArray(), defaultIdx, false, true);
        if (selected < 0) return new(SelectChoice.Cancel, null);
        if (selected < accounts.Length) return new(SelectChoice.Selected, accounts[selected]);
        if (selected == accounts.Length) return new(SelectChoice.AddAccount, null);
        return new(SelectChoice.DeleteAccount, null);
    }

    public static string? ShowDeleteAccountMenu()
    {
        var deletable = AgyAccountCore.GetAccounts().Where(a => !string.Equals(a, "default", StringComparison.OrdinalIgnoreCase)).ToArray();
        if (deletable.Length == 0)
        {
            SpectrePanel.Warning("No secondary accounts available to delete.");
            return null;
        }
        var idx = SpectreMenu.ShowRobust(["Delete Antigravity Account"], deletable, 0, false, true);
        return idx >= 0 ? deletable[idx] : null;
    }
}

public static class AgyAccountDisplay
{
    public static void ShowQuotaChart(string accountName)
    {
        var quota = AgyAccountCore.CalculateRollingQuotas(accountName);
        AnsiConsole.Write(new Rule($"[bold cyan]Quota: {accountName.EscapeMarkup()}[/]").RuleStyle("grey"));
        var chart = new BarChart().Width(60).Label($"[bold]Remaining Quota % — {accountName.EscapeMarkup()}[/]").CenterLabel().AddItem("Gemini Weekly", quota.RemainingWeekly, Color.Cyan1).AddItem("Gemini 5-Hour", quota.Remaining5H, Color.Yellow).AddItem("Claude Weekly", 100.0, Color.Green).AddItem("Claude 5-Hour", 100.0, Color.Blue);
        AnsiConsole.Write(chart);
        AnsiConsole.MarkupLine($"[dim] Weekly : {quota.CountWeekly,4} / 1000 requests · Refreshes in {quota.TimeWeekly}[/]");
        AnsiConsole.MarkupLine($"[dim] 5-Hour : {quota.Count5H,4} / 50 requests · Refreshes in {quota.Time5H}[/]");
    }

    public static void ShowAccountTree()
    {
        var accounts = AgyAccountCore.GetAccounts();
        var active = AgyAccountCore.GetActiveAccount();
        var tree = new Tree("[bold cyan]AGY Accounts[/]");
        foreach (var acc in accounts)
        {
            var stats = AgyAccountCore.GetAccountStats(acc);
            var label = acc == active ? $"[green bold]★ {acc.EscapeMarkup()} (Active)[/]" : acc.EscapeMarkup();
            var node = tree.AddNode(label);
            node.AddNode($"[dim]Login:[/] {(stats.TokenStatus == "Logged In" ? "[green]Logged In[/]" : "[red]Not Logged In[/]")}");
            node.AddNode($"[dim]Convos:[/] {stats.ConversationsCount} [dim]Skills:[/] {stats.SkillsCount}");
            node.AddNode($"[dim]Weekly:[/] {(int)Math.Round(stats.GeminiWeekly)}% [dim]5h:[/] {(int)Math.Round(stats.GeminiFiveHour)}%");
            node.AddNode($"[dim]Size:[/] {stats.PrivateSize} [dim]Junctions:[/] {stats.JunctionStatus.EscapeMarkup()}");
        }
        AnsiConsole.Write(tree);
    }

    public static string[] MultiSelectAccounts(string prompt = "Select accounts:")
    {
        var accounts = AgyAccountCore.GetAccounts();
        if (accounts.Length == 0)
        {
            SpectrePanel.Warning("No accounts found.");
            return [];
        }
        try
        {
            return [.. AnsiConsole.Prompt(new MultiSelectionPrompt<string>().Title(prompt).PageSize(12).HighlightStyle(new Style(Color.Green)).InstructionsText("[grey](Space to select · Enter to confirm)[/]").AddChoices(accounts))];
        }
        catch
        {
            return [];
        }
    }

    public static void BulkAccountOperation(string label, string selectPrompt, Action<string> action)
    {
        var selected = MultiSelectAccounts(selectPrompt);
        if (selected.Length == 0)
        {
            SpectrePanel.Info("Nothing selected.");
            return;
        }
        SpectreProgress.BulkProgress(label, selected, (_, acc) => action(acc));
    }
}

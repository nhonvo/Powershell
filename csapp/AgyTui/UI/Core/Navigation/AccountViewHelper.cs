namespace AgyTui.UI.Core.Navigation;

public static class AgyAccountDisplay
{
    private static IAgyAccountStore GetStore(IAgyAccountStore? store) => store ?? new AgyAccountStore();
    private static IAgyQuotaEngine GetEngine(IAgyQuotaEngine? engine, IAgyAccountStore store) => engine ?? new AgyQuotaEngine(store);

    public static void ShowQuotaChart(string accountName, IAgyQuotaEngine? quotaEngine = null)
    {
        var store = new AgyAccountStore();
        var engine = GetEngine(quotaEngine, store);
        var quota = engine.CalculateRollingQuotas(accountName);
        AnsiConsole.Write(new Rule($"[bold cyan]Quota: {accountName.EscapeMarkup()}[/]").RuleStyle("grey"));
        var chart = new BarChart().Width(60).Label($"[bold]Remaining Quota % — {accountName.EscapeMarkup()}[/]").CenterLabel().AddItem("Gemini Weekly", quota.RemainingWeekly, Color.Cyan1).AddItem("Gemini 5-Hour", quota.Remaining5H, Color.Yellow).AddItem("Claude Weekly", 100.0, Color.Green).AddItem("Claude 5-Hour", 100.0, Color.Blue);
        AnsiConsole.Write(chart);
        AnsiConsole.MarkupLine($"[dim] Weekly : {quota.CountWeekly,4} / 1000 requests · Refreshes in {quota.TimeWeekly}[/]");
        AnsiConsole.MarkupLine($"[dim] 5-Hour : {quota.Count5H,4} / 50 requests · Refreshes in {quota.Time5H}[/]");
    }

    public static void ShowAccountTree(IAgyAccountStore? store = null, IAgyQuotaEngine? quotaEngine = null)
    {
        var s = GetStore(store);
        var q = GetEngine(quotaEngine, s);
        var accounts = s.GetAccounts();
        var active = s.GetActiveAccount();
        var tree = new Tree("[bold cyan]AGY Accounts[/]");
        foreach (var acc in accounts)
        {
            var stats = q.GetAccountStats(acc);
            var label = acc == active ? $"[green bold]★ {acc.EscapeMarkup()} (Active)[/]" : acc.EscapeMarkup();
            var node = tree.AddNode(label);
            node.AddNode($"[dim]Login:[/] {(stats.TokenStatus == "Logged In" ? "[green]Logged In[/]" : "[red]Not Logged In[/]")}");
            node.AddNode($"[dim]Convos:[/] {stats.ConversationsCount} [dim]Skills:[/] {stats.SkillsCount}");
            node.AddNode($"[dim]Weekly:[/] {(int)Math.Round(stats.GeminiWeekly)}% [dim]5h:[/] {(int)Math.Round(stats.GeminiFiveHour)}%");
            node.AddNode($"[dim]Size:[/] {stats.PrivateSize} [dim]Junctions:[/] {stats.JunctionStatus.EscapeMarkup()}");
        }
        AnsiConsole.Write(tree);
    }

    public static string[] MultiSelectAccounts(IAgyAccountStore? store = null, string prompt = "Select accounts:")
    {
        var s = GetStore(store);
        var accounts = s.GetAccounts();
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
}

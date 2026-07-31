using AgyTui.Infrastructure.Di;
using Microsoft.Extensions.DependencyInjection;

namespace AgyTui.UI.Core.Navigation;

public static class AgyAccountDisplay
{
    private static readonly Func<IAgyAccountStore> AccountStoreFactory = () => Bootstrapper.ServiceProvider.GetRequiredService<IAgyAccountStore>();
    private static readonly Func<IAgyQuotaEngine> QuotaEngineFactory = () => Bootstrapper.ServiceProvider.GetRequiredService<IAgyQuotaEngine>();
    private static IAgyAccountStore AccountStore => AccountStoreFactory();
    private static IAgyQuotaEngine QuotaEngine => QuotaEngineFactory();

    public static void ShowQuotaChart(string accountName)
    {
        var quota = QuotaEngine.CalculateRollingQuotas(accountName);
        AnsiConsole.Write(new Rule($"[bold cyan]Quota: {accountName.EscapeMarkup()}[/]").RuleStyle("grey"));
        var chart = new BarChart().Width(60).Label($"[bold]Remaining Quota % — {accountName.EscapeMarkup()}[/]").CenterLabel().AddItem("Gemini Weekly", quota.RemainingWeekly, Color.Cyan1).AddItem("Gemini 5-Hour", quota.Remaining5H, Color.Yellow).AddItem("Claude Weekly", 100.0, Color.Green).AddItem("Claude 5-Hour", 100.0, Color.Blue);
        AnsiConsole.Write(chart);
        AnsiConsole.MarkupLine($"[dim] Weekly : {quota.CountWeekly,4} / 1000 requests · Refreshes in {quota.TimeWeekly}[/]");
        AnsiConsole.MarkupLine($"[dim] 5-Hour : {quota.Count5H,4} / 50 requests · Refreshes in {quota.Time5H}[/]");
    }

    public static void ShowAccountTree()
    {
        var accounts = AccountStore.GetAccounts();
        var active = AccountStore.GetActiveAccount();
        var tree = new Tree("[bold cyan]AGY Accounts[/]");
        foreach (var acc in accounts)
        {
            var stats = QuotaEngine.GetAccountStats(acc);
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
        var accounts = AccountStore.GetAccounts();
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

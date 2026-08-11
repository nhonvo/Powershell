using AgyTui.Infrastructure.Integrations.AgyClient.Interfaces;
using AgyTui.UI.Core.Navigation.Abstractions;
using AgyTui.UI.Core.Abstractions;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace AgyTui.UI.Screens.Workspace.Helpers;

public class AccountScreen : IScreenView
{
    private readonly IAgyAccountStore _accountStore;
    private readonly IAgyQuotaEngine _quotaEngine;

    public AccountScreen(IAgyAccountStore accountStore, IAgyQuotaEngine quotaEngine)
    {
        _accountStore = accountStore ?? throw new ArgumentNullException(nameof(accountStore));
        _quotaEngine = quotaEngine ?? throw new ArgumentNullException(nameof(quotaEngine));
    }

    public string ScreenKey => "agyswitch";
    public string Title => "AGYSWITCH Account Manager";

    public int GetItemCount(string searchFilter)
    {
        var allAccs = _accountStore.GetAccounts();
        if (string.IsNullOrEmpty(searchFilter)) return allAccs.Length;
        return allAccs.Count(a => a.Contains(searchFilter, StringComparison.OrdinalIgnoreCase));
    }

    public IRenderable Render(Grid grid, ScreenState state)
    {
        var allAccs = _accountStore.GetAccounts();
        var accs = string.IsNullOrEmpty(state.SearchFilter)
            ? allAccs
            : allAccs.Where(a => a.Contains(state.SearchFilter, StringComparison.OrdinalIgnoreCase)).ToArray();
        var activeAcc = _accountStore.GetActiveAccount();

        for (var i = 0; i < accs.Length; i++)
        {
            var isSelected = (i == state.SelectedIndex);
            var isActive = string.Equals(accs[i], activeAcc, StringComparison.OrdinalIgnoreCase);
            var prefix = isSelected ? "[green bold]> [/]" : "  ";
            var suffix = isActive ? " [bold green](Active)[/]" : "";
            var displayName = accs[i];
            var email = _accountStore.GetAccountEmail(accs[i]);
            if (!string.IsNullOrEmpty(email)) displayName = $"{accs[i]} ({email})";
            var stats = _quotaEngine.GetAccountStats(accs[i]);
            var loginStatus = stats.TokenStatus == "Logged In" ? "[green]✔ Logged In[/]" : "[red]✘ Logged Out[/]";
            var keySig = _accountStore.GetShortCredentialSignature(accs[i]);
            var keyDisplay = keySig != "None" ? $"[yellow]Key: {keySig.EscapeMarkup()}[/]" : "[dim]Key: None[/]";
            grid.AddRow(new Markup($"{prefix}{displayName.EscapeMarkup()} [dim]({loginStatus} · {keyDisplay})[/]{suffix}"));
        }

        string filterInfo = !string.IsNullOrEmpty(state.SearchFilter) ? $" [yellow]Filter: {state.SearchFilter.EscapeMarkup()}[/]" : "";
        grid.AddRow(new Markup($"\n[bold cyan]Title: 💼 Account Manager > AGYSWITCH Account Manager (agysw){filterInfo}[/]"));
        grid.AddRow(new Markup("[dim]Nav: ↑/↓ Move  │  Enter Switch  │  [[a]] Create  │  [[l]] Auth Login  │  [[d]] Delete  │  Esc Back[/]"));
        grid.AddRow(new Markup("[bold white]Select option: [/]"));
        return grid;
    }

    public ScreenNavigationResult HandleInput(ConsoleKeyInfo key, ScreenState state)
    {
        var allAccs = _accountStore.GetAccounts();
        var accs = string.IsNullOrEmpty(state.SearchFilter)
            ? allAccs
            : allAccs.Where(a => a.Contains(state.SearchFilter, StringComparison.OrdinalIgnoreCase)).ToArray();

        switch (key.Key)
        {
            case ConsoleKey.Enter:
                if (state.SelectedIndex >= 0 && state.SelectedIndex < accs.Length)
                {
                    var targetAcc = accs[state.SelectedIndex];
                    _accountStore.SetActiveAccount(targetAcc, false);
                    var stats = _quotaEngine.GetAccountStats(targetAcc);
                    if (stats.TokenStatus != "Logged In")
                    {
                        AnsiConsole.Clear();
                        SpectrePanel.Warning($"Account '{targetAcc}' is currently logged out.");
                        if (AnsiConsole.Confirm("Would you like to launch the authentication login page now?"))
                        {
                            _accountStore.AuthenticateAccount(targetAcc);
                        }
                    }
                    return new ScreenNavigationResult(NavigationAction.Handled);
                }
                break;

            case ConsoleKey.A:
                Console.CursorVisible = true;
                AnsiConsole.Clear();
                var newName = AnsiConsole.Ask<string>("Enter new account name:").Trim();
                if (!string.IsNullOrEmpty(newName))
                {
                    try
                    {
                        _accountStore.AddAccount(newName);
                        SpectrePanel.Success($"Account '{newName}' created successfully!");
                        Thread.Sleep(1200);
                    }
                    catch (Exception ex)
                    {
                        SpectrePanel.Error($"Failed to create account: {ex.Message}");
                        Thread.Sleep(1500);
                    }
                }
                Console.CursorVisible = false;
                return new ScreenNavigationResult(NavigationAction.Handled);

            case ConsoleKey.D:
                if (state.SelectedIndex >= 0 && state.SelectedIndex < accs.Length)
                {
                    var targetAcc = accs[state.SelectedIndex];
                    if (string.Equals(targetAcc, "default", StringComparison.OrdinalIgnoreCase))
                    {
                        SpectrePanel.Error("Cannot delete default account.");
                        Thread.Sleep(1200);
                        return new ScreenNavigationResult(NavigationAction.Handled);
                    }
                    Console.CursorVisible = true;
                    AnsiConsole.Clear();
                    if (AnsiConsole.Confirm($"Are you sure you want to delete account '{targetAcc}'?"))
                    {
                        _accountStore.DeleteAccount(targetAcc);
                        SpectrePanel.Success($"Account '{targetAcc}' deleted successfully!");
                        Thread.Sleep(1200);
                    }
                    Console.CursorVisible = false;
                    return new ScreenNavigationResult(NavigationAction.Handled);
                }
                break;

            case ConsoleKey.L:
                if (state.SelectedIndex >= 0 && state.SelectedIndex < accs.Length)
                {
                    var targetAcc = accs[state.SelectedIndex];
                    Console.CursorVisible = true;
                    AnsiConsole.Clear();
                    _accountStore.AuthenticateAccount(targetAcc);
                    Console.CursorVisible = false;
                    return new ScreenNavigationResult(NavigationAction.Handled);
                }
                break;

            case ConsoleKey.O:
                if (state.SelectedIndex >= 0 && state.SelectedIndex < accs.Length)
                {
                    var targetAcc = accs[state.SelectedIndex];
                    Console.CursorVisible = true;
                    AnsiConsole.Clear();
                    if (AnsiConsole.Confirm($"Are you sure you want to log out of '{targetAcc}'?"))
                    {
                        _accountStore.LogoutAccount(targetAcc);
                        SpectrePanel.Success($"Logged out of '{targetAcc}' successfully!");
                        Thread.Sleep(1200);
                    }
                    Console.CursorVisible = false;
                    return new ScreenNavigationResult(NavigationAction.Handled);
                }
                break;

            case ConsoleKey.R:
                Console.CursorVisible = true;
                AnsiConsole.Clear();
                if (AnsiConsole.Confirm("Are you sure you want to purge all custom accounts and reset to default?"))
                {
                    _accountStore.PurgeAllNonDefaultAccounts();
                    SpectrePanel.Success("All custom accounts purged. Reset active context to clean default account.");
                    Thread.Sleep(1200);
                }
                Console.CursorVisible = false;
                return new ScreenNavigationResult(NavigationAction.Handled);

            case ConsoleKey.Escape:
                return new ScreenNavigationResult(NavigationAction.Exit);
        }

        return new ScreenNavigationResult(NavigationAction.Continue);
    }
}


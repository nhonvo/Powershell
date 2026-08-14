using Spectre.Console.Rendering;

namespace AgyTui.UI.Screens.Customization.Navigators;

public static class SubPageAccountNavigator
{
    private static IAgyAccountStore GetStore(IAgyAccountStore? store) => store ?? new AgyAccountStore();
    private static IAgyQuotaEngine GetEngine(IAgyQuotaEngine? engine, IAgyAccountStore store) => engine ?? new AgyQuotaEngine(store);

    private static void ClearScreen()
    {
        try { AnsiConsole.Clear(); } catch { }
    }

    private static void SetCursorVisible(bool visible)
    {
        try { Console.CursorVisible = visible; } catch { }
    }

    public static bool HandleSelection(string searchBuffer, int detailsSel, IAgyAccountStore? AccountStore = null, IAgyQuotaEngine? QuotaEngine = null)
    {
        var store = GetStore(AccountStore);
        var quotaEngine = GetEngine(QuotaEngine, store);
        var accs = store.GetAccounts();
        if (!string.IsNullOrEmpty(searchBuffer))
        {
            accs = accs.Where(a => a.Contains(searchBuffer, StringComparison.OrdinalIgnoreCase)).ToArray();
        }
        if (detailsSel < 0 || detailsSel >= accs.Length) return false;

        var targetAcc = accs[detailsSel];
        SetCursorVisible(true);
        store.SetActiveAccount(targetAcc, false);
        var stats = quotaEngine.GetAccountStats(targetAcc);
        if (stats.TokenStatus != "Logged In")
        {
            ClearScreen();
            SpectrePanel.Warning($"Account '{targetAcc}' is currently logged out.");
            var confirm = AnsiConsole.Confirm("Would you like to launch the authentication login page now?");
            if (confirm)
            {
                store.AuthenticateAccount(targetAcc);
                SetCursorVisible(false);
                return true;
            }
            else
            {
                SetCursorVisible(false);
                return false;
            }
        }
        Thread.Sleep(800);
        SetCursorVisible(false);
        return true;
    }

    public static void CreateAccount(IAgyAccountStore? AccountStore = null)
    {
        var store = GetStore(AccountStore);
        SetCursorVisible(true);
        ClearScreen();
        var newName = AnsiConsole.Ask<string>("Enter new account name:").Trim();
        if (!string.IsNullOrEmpty(newName))
        {
            try
            {
                store.AddAccount(newName);
                SpectrePanel.Success($"Account '{newName}' created successfully!");
                Thread.Sleep(1500);
            }
            catch (Exception ex)
            {
                SpectrePanel.Error($"Failed to create account: {ex.Message}");
                Thread.Sleep(2000);
            }
        }
        SetCursorVisible(false);
    }

    public static void DeleteAccount(string searchBuffer, int detailsSel, IAgyAccountStore? AccountStore = null)
    {
        var store = GetStore(AccountStore);
        var accs = store.GetAccounts();
        if (!string.IsNullOrEmpty(searchBuffer))
        {
            accs = accs.Where(a => a.Contains(searchBuffer, StringComparison.OrdinalIgnoreCase)).ToArray();
        }
        if (detailsSel < 0 || detailsSel >= accs.Length) return;

        var targetAcc = accs[detailsSel];
        if (string.Equals(targetAcc, "default", StringComparison.OrdinalIgnoreCase))
        {
            SpectrePanel.Error("Cannot delete default account.");
            Thread.Sleep(1500);
            return;
        }
        SetCursorVisible(true);
        ClearScreen();
        var confirm = AnsiConsole.Confirm($"Are you sure you want to delete account '{targetAcc}'?");
        if (confirm)
        {
            try
            {
                store.DeleteAccount(targetAcc);
                SpectrePanel.Success($"Account '{targetAcc}' deleted successfully!");
                Thread.Sleep(1500);
            }
            catch (Exception ex)
            {
                SpectrePanel.Error($"Failed to delete account '{targetAcc}': {ex.Message}");
                Thread.Sleep(2500);
            }
        }
        SetCursorVisible(false);
    }

    public static void LoginAccount(string searchBuffer, int detailsSel, IAgyAccountStore? AccountStore = null)
    {
        var store = GetStore(AccountStore);
        var accs = store.GetAccounts();
        if (!string.IsNullOrEmpty(searchBuffer))
        {
            accs = accs.Where(a => a.Contains(searchBuffer, StringComparison.OrdinalIgnoreCase)).ToArray();
        }
        if (detailsSel < 0 || detailsSel >= accs.Length) return;

        var targetAcc = accs[detailsSel];
        SetCursorVisible(true);
        ClearScreen();
        store.AuthenticateAccount(targetAcc);
        SetCursorVisible(false);
    }

    public static void PurgeAccounts(IAgyAccountStore? AccountStore = null)
    {
        var store = GetStore(AccountStore);
        SetCursorVisible(true);
        ClearScreen();
        var confirm = AnsiConsole.Confirm("Are you sure you want to purge all custom accounts and reset to default?");
        if (confirm)
        {
            store.PurgeAllNonDefaultAccounts();
            SpectrePanel.Success("All custom accounts purged. Reset active context to clean default account.");
            Thread.Sleep(1500);
        }
        SetCursorVisible(false);
    }

    public static void LogoutAccount(string searchBuffer, int detailsSel, IAgyAccountStore? AccountStore = null)
    {
        var store = GetStore(AccountStore);
        var accs = store.GetAccounts();
        if (!string.IsNullOrEmpty(searchBuffer))
        {
            accs = accs.Where(a => a.Contains(searchBuffer, StringComparison.OrdinalIgnoreCase)).ToArray();
        }
        if (detailsSel < 0 || detailsSel >= accs.Length) return;

        var targetAcc = accs[detailsSel];
        SetCursorVisible(true);
        ClearScreen();
        var confirm = AnsiConsole.Confirm($"Are you sure you want to log out of '{targetAcc}'?");
        if (confirm)
        {
            store.LogoutAccount(targetAcc);
            SpectrePanel.Success($"Logged out of '{targetAcc}' successfully!");
            Thread.Sleep(1500);
        }
        SetCursorVisible(false);
    }

    public static IRenderable Render(Grid grid, string searchBuffer, int selIdx, IAgyAccountStore? AccountStore = null, IAgyQuotaEngine? QuotaEngine = null)
    {
        var store = GetStore(AccountStore);
        var quotaEngine = GetEngine(QuotaEngine, store);
        var allAccs = store.GetAccounts();
        var accs = string.IsNullOrEmpty(searchBuffer)
            ? allAccs
            : allAccs.Where(a => a.Contains(searchBuffer, StringComparison.OrdinalIgnoreCase)).ToArray();
        var activeAcc = store.GetActiveAccount();
        for (var i = 0; i < accs.Length; i++)
        {
            var isSelected = (i == selIdx);
            var isActive = string.Equals(accs[i], activeAcc, StringComparison.OrdinalIgnoreCase);
            var prefix = isSelected ? "[green bold]> [/]" : "  ";
            var suffix = isActive ? " [bold green](Active)[/]" : "";
            var displayName = accs[i];
            var email = store.GetAccountEmail(accs[i]);
            if (!string.IsNullOrEmpty(email)) displayName = $"{accs[i]} ({email})";
            var stats = quotaEngine.GetAccountStats(accs[i]);
            var loginStatus = stats?.TokenStatus == "Logged In" ? "[green]✔ Logged In[/]" : "[red]✘ Logged Out[/]";
            var keySig = store.GetShortCredentialSignature(accs[i]);
            var keyDisplay = keySig != "None" ? $"[yellow]Key: {keySig.EscapeMarkup()}[/]" : "[dim]Key: None[/]";
            grid.AddRow(new Markup($"{prefix}{displayName.EscapeMarkup()} [dim]({loginStatus} · {keyDisplay})[/]{suffix}"));
        }

        string filterInfo = !string.IsNullOrEmpty(searchBuffer) ? $" [yellow]Filter: {searchBuffer.EscapeMarkup()}[/]" : "";
        grid.AddRow(new Markup($"\n[bold cyan]Title: 💼 Account Manager > AGYSWITCH Account Manager (agysw){filterInfo}[/]"));
        grid.AddRow(new Markup("[dim]Nav: ↑/↓ Move  │  Enter Switch  │  [[a]] Create  │  [[l]] Auth Login  │  [[o]] Logout  │  [[d]] Delete  │  Esc Back[/]"));
        grid.AddRow(new Markup("[bold white]Select option: [/]"));
        return grid;
    }
}

using AgyTui.Infrastructure.Di;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Rendering;

namespace AgyTui.UI.Core.Navigation;

public static class SubPageAccountNavigator
{
    private static readonly Func<IAgyAccountStore> AccountStoreFactory = () => Bootstrapper.ServiceProvider.GetRequiredService<IAgyAccountStore>();
    private static readonly Func<IAgyQuotaEngine> QuotaEngineFactory = () => Bootstrapper.ServiceProvider.GetRequiredService<IAgyQuotaEngine>();
    private static IAgyAccountStore AccountStore => AccountStoreFactory();
    private static IAgyQuotaEngine QuotaEngine => QuotaEngineFactory();

    public static bool HandleSelection(string searchBuffer, int detailsSel)
    {
        var accs = AccountStore.GetAccounts();
        if (!string.IsNullOrEmpty(searchBuffer))
        {
            accs = accs.Where(a => a.Contains(searchBuffer, StringComparison.OrdinalIgnoreCase)).ToArray();
        }
        if (detailsSel < 0 || detailsSel >= accs.Length) return false;

        var targetAcc = accs[detailsSel];
        Console.CursorVisible = true;
        AccountStore.SetActiveAccount(targetAcc, false);
        var stats = QuotaEngine.GetAccountStats(targetAcc);
        if (stats.TokenStatus != "Logged In")
        {
            AnsiConsole.Clear();
            SpectrePanel.Warning($"Account '{targetAcc}' is currently logged out.");
            var confirm = AnsiConsole.Confirm("Would you like to launch the authentication login page now?");
            if (confirm)
            {
                AccountStore.AuthenticateAccount(targetAcc);
            }
        }
        Console.CursorVisible = false;
        return true;
    }

    public static void CreateAccount()
    {
        Console.CursorVisible = true;
        AnsiConsole.Clear();
        var newName = AnsiConsole.Ask<string>("Enter new account name:").Trim();
        if (!string.IsNullOrEmpty(newName))
        {
            try
            {
                AccountStore.AddAccount(newName);
                SpectrePanel.Success($"Account '{newName}' created successfully!");
                Thread.Sleep(1500);
            }
            catch (Exception ex)
            {
                SpectrePanel.Error($"Failed to create account: {ex.Message}");
                Thread.Sleep(2000);
            }
        }
        Console.CursorVisible = false;
    }

    public static void DeleteAccount(string searchBuffer, int detailsSel)
    {
        var accs = AccountStore.GetAccounts();
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
        Console.CursorVisible = true;
        AnsiConsole.Clear();
        var confirm = AnsiConsole.Confirm($"Are you sure you want to delete account '{targetAcc}'?");
        if (confirm)
        {
            AccountStore.DeleteAccount(targetAcc);
            SpectrePanel.Success($"Account '{targetAcc}' deleted successfully!");
            Thread.Sleep(1500);
        }
        Console.CursorVisible = false;
    }

    public static void LoginAccount(string searchBuffer, int detailsSel)
    {
        var accs = AccountStore.GetAccounts();
        if (!string.IsNullOrEmpty(searchBuffer))
        {
            accs = accs.Where(a => a.Contains(searchBuffer, StringComparison.OrdinalIgnoreCase)).ToArray();
        }
        if (detailsSel < 0 || detailsSel >= accs.Length) return;

        var targetAcc = accs[detailsSel];
        Console.CursorVisible = true;
        AnsiConsole.Clear();
        AccountStore.AuthenticateAccount(targetAcc);
        Console.CursorVisible = false;
    }

    public static void PurgeAccounts()
    {
        Console.CursorVisible = true;
        AnsiConsole.Clear();
        var confirm = AnsiConsole.Confirm("Are you sure you want to purge all custom accounts and reset to default?");
        if (confirm)
        {
            AccountStore.PurgeAllNonDefaultAccounts();
            SpectrePanel.Success("All custom accounts purged. Reset active context to clean default account.");
            Thread.Sleep(1500);
        }
        Console.CursorVisible = false;
    }

    public static void LogoutAccount(string searchBuffer, int detailsSel)
    {
        var accs = AccountStore.GetAccounts();
        if (!string.IsNullOrEmpty(searchBuffer))
        {
            accs = accs.Where(a => a.Contains(searchBuffer, StringComparison.OrdinalIgnoreCase)).ToArray();
        }
        if (detailsSel < 0 || detailsSel >= accs.Length) return;

        var targetAcc = accs[detailsSel];
        Console.CursorVisible = true;
        AnsiConsole.Clear();
        var confirm = AnsiConsole.Confirm($"Are you sure you want to log out of '{targetAcc}'?");
        if (confirm)
        {
            AccountStore.LogoutAccount(targetAcc);
            SpectrePanel.Success($"Logged out of '{targetAcc}' successfully!");
            Thread.Sleep(1500);
        }
        Console.CursorVisible = false;
    }

    public static IRenderable Render(Grid grid, string searchBuffer, int selIdx)
    {
        grid.AddRow(new Markup("[cyan bold]Select Account to Switch:[/]\n"));
        if (!string.IsNullOrEmpty(searchBuffer))
        {
            grid.AddRow(new Markup($"[yellow]Search:[/] [white]{searchBuffer.EscapeMarkup()}[/]_\n"));
        }
        var allAccs = AccountStore.GetAccounts();
        var accs = string.IsNullOrEmpty(searchBuffer)
            ? allAccs
            : allAccs.Where(a => a.Contains(searchBuffer, StringComparison.OrdinalIgnoreCase)).ToArray();
        var activeAcc = AccountStore.GetActiveAccount();
        for (var i = 0; i < accs.Length; i++)
        {
            var isSelected = (i == selIdx);
            var isActive = (accs[i] == activeAcc);
            var prefix = isSelected ? "[green bold]> [/]" : "  ";
            var suffix = isActive ? " [green](Active)[/]" : "";
            var displayName = accs[i];
            if (string.Equals(accs[i], "default", StringComparison.OrdinalIgnoreCase))
            {
                var email = AccountStore.GetAccountEmail("default");
                if (!string.IsNullOrEmpty(email)) displayName = $"default ({email})";
            }
            var stats = QuotaEngine.GetAccountStats(accs[i]);
            var loginStatus = stats.TokenStatus == "Logged In" ? "[green]✔ Logged In[/]" : "[red]✘ Logged Out[/]";
            grid.AddRow(new Markup($"{prefix}{displayName.EscapeMarkup()} [dim]({loginStatus})[/]{suffix}"));
        }
        grid.AddRow(new Markup("\n[dim]↑/↓ Navigate  ·  Enter Switch Account  ·  Esc Cancel[/]"));
        grid.AddRow(new Markup("[dim]a Create Account  ·  l Login/Auth  ·  d Delete  ·  r Reset/Purge  ·  o Log Out[/]"));
        return grid;
    }
}

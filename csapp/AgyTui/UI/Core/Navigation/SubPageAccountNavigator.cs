using Spectre.Console.Rendering;

namespace AgyTui.UI.Core.Navigation;

public static class SubPageAccountNavigator
{
    public static bool HandleSelection(string searchBuffer, int detailsSel)
    {
        var accs = AgyAccountCore.GetAccounts();
        if (!string.IsNullOrEmpty(searchBuffer))
        {
            accs = accs.Where(a => a.Contains(searchBuffer, StringComparison.OrdinalIgnoreCase)).ToArray();
        }
        if (detailsSel < 0 || detailsSel >= accs.Length) return false;

        var targetAcc = accs[detailsSel];
        Console.CursorVisible = true;
        AgyAccountCore.SetActiveAccount(targetAcc, false);
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
                AgyAccountCore.AddAccount(newName);
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
        var accs = AgyAccountCore.GetAccounts();
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
            AgyAccountCore.DeleteAccount(targetAcc);
            SpectrePanel.Success($"Account '{targetAcc}' deleted successfully!");
            Thread.Sleep(1500);
        }
        Console.CursorVisible = false;
    }

    public static void LogoutAccount(string searchBuffer, int detailsSel)
    {
        var accs = AgyAccountCore.GetAccounts();
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
            AgyAccountCore.LogoutAccount(targetAcc);
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
        var allAccs = AgyAccountCore.GetAccounts();
        var accs = string.IsNullOrEmpty(searchBuffer)
            ? allAccs
            : allAccs.Where(a => a.Contains(searchBuffer, StringComparison.OrdinalIgnoreCase)).ToArray();
        var activeAcc = AgyAccountCore.GetActiveAccount();
        for (var i = 0; i < accs.Length; i++)
        {
            var isSelected = (i == selIdx);
            var isActive = (accs[i] == activeAcc);
            var prefix = isSelected ? "[green bold]> [/]" : "  ";
            var suffix = isActive ? " [green](Active)[/]" : "";
            var displayName = accs[i];
            if (string.Equals(accs[i], "default", StringComparison.OrdinalIgnoreCase))
            {
                var email = AgyAccountCore.GetAccountEmail("default");
                if (!string.IsNullOrEmpty(email)) displayName = $"default ({email})";
            }
            var stats = AgyAccountCore.GetAccountStats(accs[i]);
            var loginStatus = stats.TokenStatus == "Logged In" ? "[green]✔[/]" : "[red]✘[/]";
            grid.AddRow(new Markup($"{prefix}{displayName.EscapeMarkup()} [dim]({loginStatus})[/]{suffix}"));
        }
        grid.AddRow(new Markup("\n[dim]↑/↓ Navigate  ·  Enter Select  ·  Esc Cancel[/]"));
        grid.AddRow(new Markup("[dim]a Create Account  ·  d Delete  ·  o Log Out[/]"));
        return grid;
    }
}

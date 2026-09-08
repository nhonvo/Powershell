using AgyTui.Infrastructure.Di;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Rendering;

namespace AgyTui.UI.Screens.Customization;

public class AccountManagerScreen : IScreenView
{
    public string ScreenKey => "agysw";
    public string Title => "Account Manager (agysw)";

    private static IAgyAccountStore? AccountStore => Bootstrapper.ServiceProvider?.GetService<IAgyAccountStore>();
    private static IAgyQuotaEngine? QuotaEngine => Bootstrapper.ServiceProvider?.GetService<IAgyQuotaEngine>();

    public int GetItemCount(string searchFilter)
    {
        var accs = AccountStore?.GetAccounts() ?? Array.Empty<string>();
        if (string.IsNullOrEmpty(searchFilter)) return accs.Length;
        return accs.Count(a => a.Contains(searchFilter, StringComparison.OrdinalIgnoreCase));
    }

    public IRenderable Render(Grid grid, ScreenState state)
    {
        return SubPageAccountNavigator.Render(grid, state.SearchFilter, state.SelectedIndex, AccountStore, QuotaEngine);
    }

    public ScreenNavigationResult HandleInput(ConsoleKeyInfo key, ScreenState state)
    {
        if (key.Key == ConsoleKey.Escape || key.Key == ConsoleKey.Q)
        {
            return new ScreenNavigationResult(NavigationAction.Exit);
        }

        if (key.Key == ConsoleKey.Enter)
        {
            bool shouldExit = SubPageAccountNavigator.HandleSelection(state.SearchFilter, state.SelectedIndex, AccountStore, QuotaEngine);
            return new ScreenNavigationResult(shouldExit ? NavigationAction.Exit : NavigationAction.Handled);
        }

        switch (key.Key)
        {
            case ConsoleKey.A:
                SubPageAccountNavigator.CreateAccount(AccountStore);
                return new ScreenNavigationResult(NavigationAction.Handled);

            case ConsoleKey.D:
                SubPageAccountNavigator.DeleteAccount(state.SearchFilter, state.SelectedIndex, AccountStore);
                return new ScreenNavigationResult(NavigationAction.Handled);

            case ConsoleKey.L:
                SubPageAccountNavigator.LoginAccount(state.SearchFilter, state.SelectedIndex, AccountStore);
                return new ScreenNavigationResult(NavigationAction.Handled);

            case ConsoleKey.O:
                SubPageAccountNavigator.LogoutAccount(state.SearchFilter, state.SelectedIndex, AccountStore);
                return new ScreenNavigationResult(NavigationAction.Handled);

            case ConsoleKey.R:
                SubPageAccountNavigator.PurgeAccounts(AccountStore);
                return new ScreenNavigationResult(NavigationAction.Handled);
        }

        return new ScreenNavigationResult(NavigationAction.Continue);
    }
}


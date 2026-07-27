using AgyTui.Core.Interfaces;
using AgyTui.Infrastructure.Di;
using Microsoft.Extensions.DependencyInjection;

namespace AgyTui.Infrastructure.Integrations.AgyClient;

public static class AccountRepository
{
    public static string AgySourceHome => AgyAccountCore.AgySourceHome;
    public static string ActiveAccountFile => Path.Combine(AgySourceHome, "active_account.txt");
    public static string AccountsDir => Path.Combine(AgySourceHome, "accounts");

    public static string GetActiveAccount() => Bootstrapper.ServiceProvider.GetRequiredService<IAccountRepository>().GetActiveAccount();

    public static void SetActiveAccount(string accountName) => Bootstrapper.ServiceProvider.GetRequiredService<IAccountRepository>().SetActiveAccount(accountName);

    public static string[] GetAccounts() => Bootstrapper.ServiceProvider.GetRequiredService<IAccountRepository>().GetAccounts();
}

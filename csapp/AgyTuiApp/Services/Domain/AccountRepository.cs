using System;
using System.Collections.Generic;
using System.IO;

namespace AgyTui;

public static class AccountRepository
{
    public static string AgySourceHome => AgyAccountCore.AgySourceHome;
    public static string ActiveAccountFile => Path.Combine(AgySourceHome, "active_account.txt");
    public static string AccountsDir => Path.Combine(AgySourceHome, "accounts");

    public static string GetActiveAccount() => AgyServices.Account.GetActiveAccount();

    public static void SetActiveAccount(string accountName) => AgyServices.Account.SetActiveAccount(accountName);

    public static string[] GetAccounts() => AgyServices.Account.GetAccounts();
}

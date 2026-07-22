using System;
using System.Collections.Generic;
using System.IO;

namespace AgyTui;

public static class AccountRepository
{
    public static string AgySourceHome => AgyAccountCore.AgySourceHome;
    public static string ActiveAccountFile => Path.Combine(AgySourceHome, "active_account.txt");
    public static string AccountsDir => Path.Combine(AgySourceHome, "accounts");

    public static string GetActiveAccount()
    {
        if (File.Exists(ActiveAccountFile))
        {
            var acc = File.ReadAllText(ActiveAccountFile).Trim();
            if (!string.IsNullOrEmpty(acc)) return acc;
        }
        return "default";
    }

    public static void SetActiveAccount(string accountName)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ActiveAccountFile)!);
        File.WriteAllText(ActiveAccountFile, accountName);

        var markerPath = Path.Combine(AgySourceHome, "last_account_change.txt");
        File.WriteAllText(markerPath, accountName);
    }

    public static string[] GetAccounts()
    {
        return AgyAccountCore.GetAccounts();
    }
}

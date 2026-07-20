using System;
using System.Collections.Generic;
using System.IO;

namespace AgyTui;

public static class AccountRepository
{
    public static string AgySourceHome => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".gemini", "antigravity");
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

        // Session continuity marker written upon active account change
        var markerPath = Path.Combine(AgySourceHome, "last_account_change.txt");
        File.WriteAllText(markerPath, accountName);
    }

    public static string[] GetAccounts()
    {
        if (!Directory.Exists(AccountsDir)) return Array.Empty<string>();
        return Directory.GetDirectories(AccountsDir);
    }
}

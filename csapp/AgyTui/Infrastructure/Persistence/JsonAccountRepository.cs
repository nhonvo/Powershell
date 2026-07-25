namespace AgyTui.Infrastructure.Persistence;

public class JsonAccountRepository : IAccountRepository
{
    private string AgySourceHome => AgyAccountCore.AgySourceHome;
    private string ActiveAccountFile => Path.Combine(AgySourceHome, "active_account.txt");

    public string GetActiveAccount()
    {
        if (File.Exists(ActiveAccountFile))
        {
            var acc = File.ReadAllText(ActiveAccountFile).Trim();
            if (!string.IsNullOrEmpty(acc)) return acc;
        }
        return "default";
    }

    public void SetActiveAccount(string accountName)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ActiveAccountFile)!);
        File.WriteAllText(ActiveAccountFile, accountName);

        var markerPath = Path.Combine(AgySourceHome, "last_account_change.txt");
        File.WriteAllText(markerPath, accountName);
    }

    public string[] GetAccounts()
    {
        return AgyAccountCore.GetAccounts();
    }
}

namespace AgyTui.Infrastructure.Persistence.Accounts;

public class JsonAccountRepository : IAccountRepository
{
    private string AgySourceHome => AgyAccountCore.AgySourceHome;
    private string ActiveAccountFile => Path.Combine(AgySourceHome, "active_account.txt");

    public string GetActiveAccount()
    {
        if (File.Exists(ActiveAccountFile))
        {
            try
            {
                var acc = File.ReadAllText(ActiveAccountFile).Trim();
                if (!string.IsNullOrEmpty(acc)) return acc;
            }
            catch { }
        }
        return "default";
    }

    public void SetActiveAccount(string accountName)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ActiveAccountFile)!);
        File.WriteAllText(ActiveAccountFile, accountName);
    }

    public string[] GetAccounts()
    {
        return AgyAccountCore.GetAccounts();
    }
}

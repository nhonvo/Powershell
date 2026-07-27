namespace AgyTui.Infrastructure.Integrations.AgyClient;

using AgyTui.Core.Interfaces;

public class AgyAccountStore : IAccountRepository
{
    private string AgySourceHome => AgyAccountCore.AgySourceHome;

    private IEnumerable<string> GetActiveAccountFileCandidates()
    {
        var list = new List<string>
        {
            Path.Combine(AgySourceHome, "active_account.txt"),
            Path.Combine(AgySourceHome, "active_account")
        };

        var userGemini = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".gemini");
        list.Add(Path.Combine(userGemini, "active_account.txt"));
        list.Add(Path.Combine(userGemini, "active_account"));

        return list.Distinct(StringComparer.OrdinalIgnoreCase);
    }

    public string GetActiveAccount()
    {
        foreach (var file in GetActiveAccountFileCandidates())
        {
            if (File.Exists(file))
            {
                try
                {
                    var acc = File.ReadAllText(file).Trim();
                    if (!string.IsNullOrEmpty(acc)) return acc;
                }
                catch { }
            }
        }
        return "default";
    }

    public void SetActiveAccount(string accountName)
    {
        var targetDir = AgyAccountCore.GetAccountDirectory(accountName);

        try
        {
            Environment.SetEnvironmentVariable("GEMINI_HOME", targetDir, EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable("GEMINI_HOME", targetDir, EnvironmentVariableTarget.User);
        }
        catch { }

        foreach (var file in GetActiveAccountFileCandidates())
        {
            try
            {
                var dir = Path.GetDirectoryName(file);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(file, accountName);
            }
            catch { }
        }
    }

    public string[] GetAccounts()
    {
        return AgyAccountCore.GetAccounts();
    }
}

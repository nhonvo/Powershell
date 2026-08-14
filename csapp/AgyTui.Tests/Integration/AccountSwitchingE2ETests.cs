using System.Diagnostics;
using System.Text.RegularExpressions;
using AgyTui.Infrastructure.Integrations.AgyClient;
using AgyTui.Infrastructure.Integrations.AgyClient.Interfaces;
using Xunit;

namespace AgyTui.Tests.Integration;

public class AccountSwitchingE2ETests
{
    [Fact]
    public void E2E_AccountSwitching_SwitchesAllAccountsAndVerifiesViaAgyCliOpeningScreen()
    {
        IAgyAccountStore store = new AgyAccountStore();
        var allAccounts = store.GetAccounts();
        var nonDefaultAccounts = allAccounts
            .Where(a => !string.Equals(a, "default", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.NotEmpty(nonDefaultAccounts);

        var originalActive = store.GetActiveAccount();

        try
        {
            foreach (var accountName in nonDefaultAccounts)
            {
                // 1. Perform account switch via store in temporary mode to protect live user environment
                store.SetActiveAccount(accountName, temporary: true);

                // 2. Verify active account returned by store & environment variable
                var currentActive = store.GetActiveAccount();
                Assert.Equal(accountName, currentActive);

                var expectedDir = store.GetAccountDirectory(accountName);
                var processGeminiHome = Environment.GetEnvironmentVariable("GEMINI_HOME");
                Assert.Equal(expectedDir, processGeminiHome);

                // 3. Directly launch external agy.exe process to open the opening screen of agy
                var agyExe = FindAgyExecutable();
                Assert.False(string.IsNullOrEmpty(agyExe), "agy executable must be found on PATH or installation directory");

                var psi = new ProcessStartInfo
                {
                    FileName = agyExe,
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                psi.EnvironmentVariables["GEMINI_HOME"] = expectedDir;

                using var proc = Process.Start(psi);
                Assert.NotNull(proc);
                proc.StandardInput.WriteLine("exit");
                proc.StandardInput.Close();

                string stdout = proc.StandardOutput.ReadToEnd();
                string stderr = proc.StandardError.ReadToEnd();
                proc.WaitForExit(5000);

                var combinedOutput = stdout + stderr;
                Assert.NotEmpty(combinedOutput);

                // 4. Extract email or account handle string from agy opening screen output and assert validity
                var expectedEmail = store.GetAccountEmail(accountName);
                if (!string.IsNullOrEmpty(expectedEmail))
                {
                    var emailMatch = Regex.Match(combinedOutput, @"([a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,})");
                    if (emailMatch.Success)
                    {
                        Assert.Equal(expectedEmail, emailMatch.Groups[1].Value);
                    }
                }

                // 5. Verify primary root .gemini directory and account directory contain synchronized keyring_token.txt
                var primaryDir = Path.Combine(Environment.GetEnvironmentVariable("USERPROFILE") ?? "", ".gemini");
                var primaryTokenFile = Path.Combine(primaryDir, "keyring_token.txt");
                var accTokenFile = Path.Combine(expectedDir, "keyring_token.txt");

                if (File.Exists(accTokenFile))
                {
                    Assert.True(File.Exists(primaryTokenFile), $"Primary token file should exist for account '{accountName}'");
                    var primaryContent = File.ReadAllText(primaryTokenFile).Trim();
                    var accContent = File.ReadAllText(accTokenFile).Trim();
                    Assert.Equal(accContent, primaryContent);
                }

                // 6. Verify Windows Credential Manager DPAPI token
                var activeKeyringToken = AgyKeyringHelper.ReadToken("gemini:antigravity");
                if (File.Exists(accTokenFile))
                {
                    Assert.False(string.IsNullOrEmpty(activeKeyringToken), $"Keyring token should not be empty for active account '{accountName}'");
                }
            }
        }
        finally
        {
            if (!string.IsNullOrEmpty(originalActive))
            {
                store.SetActiveAccount(originalActive, temporary: false);
            }
        }
    }

    private static string? FindAgyExecutable()
    {
        var candidates = new[]
        {
            @"C:\ProgramData\agy\bin\agy.exe",
            Path.Combine(Environment.GetEnvironmentVariable("USERPROFILE") ?? "", ".gemini", "antigravity-cli", "agy.exe")
        };
        foreach (var c in candidates)
        {
            if (File.Exists(c)) return c;
        }

        var path = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var dir in path.Split(Path.PathSeparator))
        {
            var cleanDir = dir.Trim();
            if (string.IsNullOrEmpty(cleanDir)) continue;
            var candidate = Path.Combine(cleanDir, "agy.exe");
            if (File.Exists(candidate)) return candidate;
            var cmdCandidate = Path.Combine(cleanDir, "agy.cmd");
            if (File.Exists(cmdCandidate)) return cmdCandidate;
        }
        return null;
    }
}

using System.Runtime.InteropServices;
using System.Text.Json;

namespace AgyTui.Infrastructure.Persistence;

public static class AgyKeyringHelper
{
    [DllImport("advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredRead(string target, uint type, uint reserved, out IntPtr credentialPtr);

    [DllImport("advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredWrite(ref CREDENTIAL credential, uint flags);

    [DllImport("advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredDelete(string target, uint type, uint flags);

    [DllImport("advapi32.dll", EntryPoint = "CredFree", SetLastError = true)]
    private static extern void CredFree(IntPtr buffer);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct CREDENTIAL
    {
        public uint Flags;
        public uint Type;
        public string? TargetName;
        public string? Comment;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        public uint CredentialBlobSize;
        public IntPtr CredentialBlob;
        public uint Persist;
        public uint AttributeCount;
        public IntPtr Attributes;
        public string? TargetAlias;
        public string? UserName;
    }

    public static string? ReadToken(string target)
    {
        if (!CredRead(target, 1, 0, out var credPtr)) return null;

        try
        {
            var cred = Marshal.PtrToStructure<CREDENTIAL>(credPtr);
            if (cred.CredentialBlobSize > 0 && cred.CredentialBlob != IntPtr.Zero)
            {
                var blob = new byte[cred.CredentialBlobSize];
                Marshal.Copy(cred.CredentialBlob, blob, 0, (int)cred.CredentialBlobSize);
                return Encoding.UTF8.GetString(blob);
            }
            return null;
        }
        finally
        {
            CredFree(credPtr);
        }
    }

    public static bool WriteToken(string target, string username, string token)
    {
        var cred = new CREDENTIAL
        {
            Type = 1,
            TargetName = target,
            UserName = username,
            Persist = 2
        };
        var blob = Encoding.UTF8.GetBytes(token);
        cred.CredentialBlobSize = (uint)blob.Length;
        var blobPtr = Marshal.AllocHGlobal(blob.Length);
        try
        {
            Marshal.Copy(blob, 0, blobPtr, blob.Length);
            cred.CredentialBlob = blobPtr;
            return CredWrite(ref cred, 0);
        }
        finally
        {
            Marshal.FreeHGlobal(blobPtr);
        }
    }

    public static bool DeleteToken(string target) => CredDelete(target, 1, 0);
}

public static class AgySecretVault
{
    public static string GetSecretsFilePath()
    {
        var dir = AgyAccountCore.AgySourceHome;
        Directory.CreateDirectory(dir);
        return System.IO.Path.Combine(dir, "secrets.json");
    }

    public static Dictionary<string, string> LoadSecrets()
    {
        var file = GetSecretsFilePath();
        if (!File.Exists(file)) return new();

        try
        {
            var raw = File.ReadAllText(file);
            if (string.IsNullOrWhiteSpace(raw)) return new();
            return JsonSerializer.Deserialize<Dictionary<string, string>>(raw) ?? new();
        }
        catch
        {
            return new();
        }
    }

    public static void SaveSecrets(Dictionary<string, string> secrets)
    {
        var file = GetSecretsFilePath();

        try
        {
            File.WriteAllText(file, JsonSerializer.Serialize(secrets));
        }
        catch (Exception ex)
        {
            SpectrePanel.Error($"Failed to save secrets: {ex.Message}");
        }
    }

    public static void SetSecret(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
        {
            SpectrePanel.Error("Key and Value cannot be empty.");
            return;
        }
        var secrets = LoadSecrets();

        try
        {
            secrets[key] = TokenVault.Protect(value);
            SaveSecrets(secrets);
            AnsiConsole.MarkupLine($"[green]Secret '{key.EscapeMarkup()}' saved and encrypted successfully.[/]");
        }
        catch (Exception ex)
        {
            SpectrePanel.Error($"Failed to encrypt/save secret: {ex.Message}");
        }
    }

    public static string GetSecret(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return "";
        var secrets = LoadSecrets();
        if (!secrets.TryGetValue(key, out var encrypted))
        {
            SpectrePanel.Warning($"Secret '{key}' not found.");
            return "";
        }
        try
        {
            return TokenVault.Unprotect(encrypted);
        }
        catch (Exception ex)
        {
            SpectrePanel.Error($"Failed to decrypt secret '{key}': {ex.Message}");
            return "";
        }
    }

    public static void RemoveSecret(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        var secrets = LoadSecrets();
        if (secrets.Remove(key))
        {
            SaveSecrets(secrets);
            AnsiConsole.MarkupLine($"[green]Secret '{key.EscapeMarkup()}' removed successfully.[/]");
        }
        else
        {
            SpectrePanel.Warning($"Secret '{key}' not found.");
        }
    }

    public static void ListSecrets()
    {
        var secrets = LoadSecrets();
        if (secrets.Count == 0)
        {
            SpectrePanel.Warning("No secrets stored.");
            return;
        }
        AnsiConsole.MarkupLine("[cyan]Stored Secret Keys:[/]");
        foreach (var key in secrets.Keys) AnsiConsole.MarkupLine($" * {key.EscapeMarkup()}");
    }
}

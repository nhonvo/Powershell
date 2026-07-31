namespace AgyTui.Domain.Common;

public static class ErrorConstants
{
    public static class Vault
    {
        public const string StorageAccessFailed = "ERR_VLT_001: Unable to access or decrypt secure vault storage.";
        public const string AccountSyncFailed = "ERR_VLT_002: Failed to synchronize account quota credentials.";
        public const string KeyDerivationError = "ERR_VLT_003: Encryption key derivation failed.";
    }

    public static class Workspace
    {
        public const string DirectoryNotFound = "ERR_WS_001: Target workspace path does not exist.";
        public const string AccessDenied = "ERR_WS_002: Permission denied accessing workspace directory.";
        public const string ConfigSaveFailed = "ERR_WS_003: Failed to persist workspace configuration.";
    }

    public static class Shell
    {
        public const string ProcessLaunchFailed = "ERR_SH_001: External terminal or process failed to launch.";
        public const string LocationChangeFailed = "ERR_SH_002: PowerShell directory change operation failed.";
    }

    public static class System
    {
        public const string GeneralError = "ERR_SYS_000: An unexpected system operation failure occurred.";
    }
}

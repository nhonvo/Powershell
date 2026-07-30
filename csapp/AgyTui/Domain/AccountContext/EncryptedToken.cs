namespace AgyTui.Domain.AccountContext;

public sealed record EncryptedToken(string AccountName, string CipherText, DateTime CreatedAtUtc);

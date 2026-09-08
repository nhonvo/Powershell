namespace AgyTui.Infrastructure.Persistence.Seeding;

public class AccountSeeder : ISeeder
{
    private readonly IAgyAccountRepository _accountRepo;

    public int Order => 1;

    public AccountSeeder(IAgyAccountRepository accountRepo)
    {
        _accountRepo = accountRepo;
    }

    public void Seed()
    {
        try
        {
            var accounts = _accountRepo.GetAccounts();
            if (accounts.Length == 0 || !accounts.Contains("default", StringComparer.OrdinalIgnoreCase))
            {
                _accountRepo.AddAccount("default");
                _accountRepo.SaveAccountMetadata("default", new AccountMetadata());
            }
        }
        catch { }
    }
}

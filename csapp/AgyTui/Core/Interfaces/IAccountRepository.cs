namespace AgyTui.Core.Interfaces;

public interface IAccountRepository
{
    string GetActiveAccount();
    void SetActiveAccount(string accountName);
    string[] GetAccounts();
}

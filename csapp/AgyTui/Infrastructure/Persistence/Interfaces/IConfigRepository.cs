namespace AgyTui.Infrastructure.Persistence.Interfaces;

public interface IConfigRepository
{
    ConfigData LoadConfig();
    void SaveConfig(ConfigData config);
    string? GetState(string key);
    void SetState(string key, string? value);
}

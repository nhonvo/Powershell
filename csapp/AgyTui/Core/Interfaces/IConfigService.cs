namespace AgyTui.Core.Interfaces;

public interface IConfigService
{
    ConfigData Current { get; }
    void Save();
    void Reload();
}

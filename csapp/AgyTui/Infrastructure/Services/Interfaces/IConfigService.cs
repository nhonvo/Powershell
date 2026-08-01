using AgyTui.Infrastructure.Configuration;

namespace AgyTui.Infrastructure.Services;

public interface IConfigService
{
    ConfigData Current { get; }
    void Save();
    void Reload();
}

using AgyTui.Infrastructure.Services;
using AgyTui.Infrastructure.Configuration;

namespace AgyTui.Infrastructure.Services;

public class ConfigService : IConfigService
{
    public ConfigData Current => Config.Current;

    public void Save()
    {
        Config.Save();
    }

    public void Reload()
    {
        Config.Load();
    }
}


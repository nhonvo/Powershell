using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using AgyTui.Infrastructure.Services;
using AgyTui.Infrastructure.Integrations.AgyClient;
using AgyTui.Infrastructure.Integrations.AgyClient.Interfaces;
using AgyTui.Infrastructure.Persistence.Interfaces;
using AgyTui.Tests.Mocks;

namespace AgyTui.Tests.Fixtures;

public class ServiceTestFixture : IDisposable
{
    public IServiceCollection Services { get; } = new ServiceCollection();
    public IServiceProvider ServiceProvider => Services.BuildServiceProvider();

    public ServiceTestFixture()
    {
        Services.AddSingleton<IAppPathManager, AppPathManager>();
        Services.AddSingleton<ISqliteDatabase, FakeSqliteDatabase>();
        Services.AddSingleton<IAgyAccountRepository, InMemoryAgyAccountRepository>();
        Services.AddSingleton<IAgyAccountStore, AgyAccountStore>();
        Services.AddSingleton<IAgyQuotaEngine, AgyQuotaEngine>();
        Services.AddSingleton<IAgyVault, AgyVault>();
    }

    public ServiceTestFixture WithMock<TService>(TService mockInstance) where TService : class
    {
        Services.RemoveAll(typeof(TService));
        Services.AddSingleton(mockInstance);
        return this;
    }

    public void Dispose()
    {
    }
}


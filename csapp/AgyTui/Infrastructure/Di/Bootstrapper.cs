using AgyTui.Infrastructure.Services;
using AgyTui.Infrastructure.Integrations.Ai.Abstractions;
using AgyTui.Infrastructure.Integrations.Ai.Providers;
using AgyTui.Infrastructure.Integrations.Ai.Services;
using AgyTui.Infrastructure.Logging;
using AgyTui.Infrastructure.Persistence;
using AgyTui.Infrastructure.Services;
using AgyTui.UI.Core.Commands;
using AgyTui.UI.Core.State;
using Microsoft.Extensions.DependencyInjection;

namespace AgyTui.Infrastructure.Di;

public static class Bootstrapper
{
    private static IServiceProvider? _serviceProvider;

    public static IServiceProvider ServiceProvider => _serviceProvider ??= BuildServiceProvider();

    public static IServiceProvider BuildServiceProvider(IServiceCollection? customServices = null)
    {
        var services = customServices ?? new ServiceCollection();

        // Core Services & Reactive UI State
        services.AddSingleton<IAppPathManager, AppPathManager>();
        services.AddSingleton<IConfigService, ConfigService>();
        services.AddSingleton<IUiStateStore, UiStateStore>();
        services.AddSingleton<IUiCommandDispatcher, UiCommandDispatcher>();

        // AI Services
        services.AddSingleton<IAiProcessRunner, AiProcessRunner>();
        services.AddSingleton<IOllamaClient, OllamaClient>();
        services.AddSingleton<IClaudeClient, ClaudeProvider>();
        services.AddSingleton<IHermesClient, HermesProvider>();
        services.AddSingleton<IOpenClawClient, OpenClawProvider>();
        services.AddSingleton<IAiProjectScanner, AiProjectScanner>();
        services.AddSingleton<IAiCommitGenerator, AiCommitGenerator>();
        services.AddSingleton<IAiLearningGenerator, AiLearningGenerator>();

        // SQLite Persistence
        services.AddSingleton<ISqliteDatabase, SqliteDatabase>();
        services.AddSingleton<SqliteMigrationEngine>();
        services.AddSingleton<IConfigRepository, SqliteConfigRepository>();
        services.AddSingleton<IAgyAccountRepository, SqliteAgyAccountRepository>();

        // Tool Integration Services
        services.AddSingleton<IAwsClient, AwsClient>();
        services.AddSingleton<IDockerClient, DockerClient>();
        services.AddSingleton<IDotNetClient, DotNetClient>();
        services.AddSingleton<IGitClient, GitClient>();
        services.AddSingleton<IAgyAccountStore, AgyAccountStore>();
        services.AddSingleton<IAgyQuotaEngine, AgyQuotaEngine>();
        services.AddSingleton<IAgyVault, AgyVault>();
        services.AddSingleton<IStudyRepository, JsonStudyRepository>();

        // Navigation, Renderers & Routers with Logging Middleware
        services.AddSingleton<ThreePaneRenderer>();
        services.AddSingleton<FlatTreeRenderer>();
        services.AddSingleton<IStatusWidget, DiskSpaceWidget>();
        services.AddSingleton<IStatusWidget, PublicIpWidget>();
        services.AddSingleton<IStatusWidget, SshInfoWidget>();
        services.AddSingleton<IStatusWidget, AccountTreeWidget>();
        services.AddSingleton<IStatusWidget, QuotaChartWidget>();
        services.AddSingleton<IStatusWidget, LiveDashboardWidget>();
        services.AddSingleton<IStatusWidget, OllamaStatusWidget>();
        services.AddSingleton<IUiNavigationHandler, UiNavigationHandler>();
        services.AddSingleton<CommandRouter>();
        services.AddSingleton<ICommandRouter>(sp => new CommandLoggingMiddleware(sp.GetRequiredService<CommandRouter>()));

        _serviceProvider = services.BuildServiceProvider();
        return _serviceProvider;
    }
}


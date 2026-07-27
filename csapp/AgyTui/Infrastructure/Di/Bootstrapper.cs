using AgyTui.Infrastructure.Integrations.Ai.Abstractions;
using AgyTui.Infrastructure.Integrations.Ai.Providers;
using AgyTui.Infrastructure.Integrations.Ai.Services;
using AgyTui.Infrastructure.Logging;
using AgyTui.Infrastructure.Persistence;
using AgyTui.Infrastructure.Persistence.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace AgyTui.Infrastructure.Di;

public static class Bootstrapper
{
    private static IServiceProvider? _serviceProvider;

    public static IServiceProvider ServiceProvider => _serviceProvider ??= BuildServiceProvider();

    public static IServiceProvider BuildServiceProvider(IServiceCollection? customServices = null)
    {
        var services = customServices ?? new ServiceCollection();

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
        services.AddSingleton<IAgyClient, AgyClient>();
        services.AddSingleton<IStudyRepository, JsonStudyRepository>();

        // Navigation & Routers with Logging Middleware
        services.AddSingleton<IUiNavigationHandler, UiNavigationHandler>();
        services.AddSingleton<CommandRouter>();
        services.AddSingleton<ICommandRouter>(sp => new CommandLoggingMiddleware(sp.GetRequiredService<CommandRouter>()));

        _serviceProvider = services.BuildServiceProvider();
        return _serviceProvider;
    }
}

using Microsoft.Extensions.DependencyInjection;
using AgyTui.Core.Interfaces;
using AgyTui.Infrastructure.Integrations.Ai.Abstractions;
using AgyTui.Infrastructure.Integrations.Ai.Providers;
using AgyTui.Infrastructure.Integrations.Ai.Services;
using AgyTui.Infrastructure.Integrations.Aws;
using AgyTui.Infrastructure.Integrations.Docker;
using AgyTui.Infrastructure.Integrations.DotNet;
using AgyTui.Infrastructure.Integrations.Git;
using AgyTui.Infrastructure.Logging;
using AgyTui.Infrastructure.Integrations.AgyClient;
using AgyTui.Infrastructure.Persistence.Learning;
using AgyTui.UI.Core.Navigation;

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

        // Tool Integration Services
        services.AddSingleton<IAwsClient, AwsClient>();
        services.AddSingleton<IDockerClient, DockerClient>();
        services.AddSingleton<IDotNetClient, DotNetClient>();
        services.AddSingleton<IGitClient, GitClient>();
        services.AddSingleton<IAgyTokenManager, AgyTokenManager>();
        services.AddSingleton<IAgyQuotaCalculator, AgyQuotaCalculator>();
        services.AddSingleton<IAgyAccountStatsProvider, AgyAccountStatsProvider>();
        services.AddSingleton<IAgyAccountSwitcher, AgyAccountSwitcher>();
        services.AddSingleton<IAgyClient, AgyClient>();

        // Repositories
        services.AddSingleton<IAccountRepository, JsonAccountRepository>();
        services.AddSingleton<IStudyRepository, JsonStudyRepository>();

        // Navigation & Routers with Logging Middleware
        services.AddSingleton<CommandRouter>();
        services.AddSingleton<ICommandRouter>(sp => new CommandLoggingMiddleware(sp.GetRequiredService<CommandRouter>()));

        _serviceProvider = services.BuildServiceProvider();
        return _serviceProvider;
    }
}

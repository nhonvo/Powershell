using Microsoft.Extensions.DependencyInjection;
using AgyTui.Infrastructure.Di;
using AgyTui.Infrastructure.Integrations.Ai.Abstractions;
using AgyTui.Infrastructure.Integrations.Aws;
using AgyTui.Infrastructure.Integrations.Docker;
using AgyTui.Infrastructure.Integrations.DotNet;
using AgyTui.Infrastructure.Integrations.Git;
using AgyTui.Infrastructure.Persistence.Accounts;
using AgyTui.Infrastructure.Persistence.Learning;

namespace AgyTui.Infrastructure;

public static class AgyServices
{
    public static IServiceProvider ServiceProvider => Bootstrapper.ServiceProvider;

    public static IAwsClient Aws => ServiceProvider.GetRequiredService<IAwsClient>();
    public static IDockerClient Docker => ServiceProvider.GetRequiredService<IDockerClient>();
    public static IDotNetClient DotNet => ServiceProvider.GetRequiredService<IDotNetClient>();
    public static IGitClient Git => ServiceProvider.GetRequiredService<IGitClient>();

    public static IOllamaClient Ollama => ServiceProvider.GetRequiredService<IOllamaClient>();
    public static IClaudeClient Claude => ServiceProvider.GetRequiredService<IClaudeClient>();
    public static IHermesClient Hermes => ServiceProvider.GetRequiredService<IHermesClient>();
    public static IOpenClawClient OpenClaw => ServiceProvider.GetRequiredService<IOpenClawClient>();
    public static IAiProjectScanner ProjectScanner => ServiceProvider.GetRequiredService<IAiProjectScanner>();
    public static IAiCommitGenerator CommitGenerator => ServiceProvider.GetRequiredService<IAiCommitGenerator>();
    public static IAiProcessRunner ProcessRunner => ServiceProvider.GetRequiredService<IAiProcessRunner>();
    public static IAiLearningGenerator LearningGenerator => ServiceProvider.GetRequiredService<IAiLearningGenerator>();

    public static IAccountRepository Account => ServiceProvider.GetRequiredService<IAccountRepository>();
    public static IStudyRepository Study => ServiceProvider.GetRequiredService<IStudyRepository>();
}

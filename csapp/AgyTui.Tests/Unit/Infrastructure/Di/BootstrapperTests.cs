using AgyTui.Core.Interfaces;
using AgyTui.Infrastructure.Di;
using AgyTui.Infrastructure.Integrations.Ai.Abstractions;
using AgyTui.Infrastructure.Integrations.Aws;
using AgyTui.Infrastructure.Integrations.Docker;
using AgyTui.Infrastructure.Integrations.DotNet;
using AgyTui.Infrastructure.Integrations.Git;
using Microsoft.Extensions.DependencyInjection;

namespace AgyTui.Tests.Unit.Infrastructure.Di;

public class BootstrapperTests
{
    [Fact]
    public void BuildServiceProvider_RegistersAllRequiredServices()
    {
        var provider = Bootstrapper.BuildServiceProvider();

        Assert.NotNull(provider.GetService<IAiProcessRunner>());
        Assert.NotNull(provider.GetService<IOllamaClient>());
        Assert.NotNull(provider.GetService<IClaudeClient>());
        Assert.NotNull(provider.GetService<IHermesClient>());
        Assert.NotNull(provider.GetService<IOpenClawClient>());
        Assert.NotNull(provider.GetService<IAiProjectScanner>());
        Assert.NotNull(provider.GetService<IAiCommitGenerator>());
        Assert.NotNull(provider.GetService<IAiLearningGenerator>());

        Assert.NotNull(provider.GetService<IAwsClient>());
        Assert.NotNull(provider.GetService<IDockerClient>());
        Assert.NotNull(provider.GetService<IDotNetClient>());
        Assert.NotNull(provider.GetService<IGitClient>());

        Assert.NotNull(provider.GetService<IAgyAccountStore>());
        Assert.NotNull(provider.GetService<IStudyRepository>());

        Assert.NotNull(provider.GetService<ICommandRouter>());
    }

    [Fact]
    public void ServiceProvider_SingletonProperty_ReturnsNonNullInstance()
    {
        var provider = Bootstrapper.ServiceProvider;
        Assert.NotNull(provider);
        Assert.Same(provider, Bootstrapper.ServiceProvider);
    }
}

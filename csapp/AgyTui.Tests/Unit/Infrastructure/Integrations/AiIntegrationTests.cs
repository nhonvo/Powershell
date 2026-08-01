using AgyTui.Infrastructure.Integrations.Ai.Abstractions;
using AgyTui.Infrastructure.Integrations.Ai.Providers;
using AgyTui.Infrastructure.Integrations.Ai.Services;
using Xunit;

namespace AgyTui.Tests.Unit.Infrastructure.Integrations;

public class AiIntegrationTests
{
    [Fact]
    public void AiCommitGenerator_GenerateDraftDescription_EmptyDiff_ReturnsDefaultMessage()
    {
        IAiCommitGenerator generator = new AiCommitGenerator();
        var result = generator.GenerateDraftDescription("");
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public void AiProcessRunner_Instance_CanBeCreated()
    {
        IAiProcessRunner runner = new AiProcessRunner();
        Assert.NotNull(runner);
    }

    [Fact]
    public void OllamaClient_Instance_CanBeCreated()
    {
        IOllamaClient client = new OllamaClient(new AiProcessRunner());
        Assert.NotNull(client);
    }

    [Fact]
    public void OpenClawClient_Instance_CanBeCreated()
    {
        IOpenClawClient client = new OpenClawProvider(new AiProcessRunner(), new OllamaClient(new AiProcessRunner()));
        Assert.NotNull(client);
    }
}

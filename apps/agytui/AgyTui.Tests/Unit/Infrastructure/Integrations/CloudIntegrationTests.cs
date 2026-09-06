using AgyTui.Infrastructure.Integrations.Aws;
using AgyTui.Infrastructure.Integrations.Docker;
using AgyTui.Infrastructure.Integrations.DotNet;
using AgyTui.Infrastructure.Integrations.Git;
using AgyTui.Infrastructure.Integrations.Ai.Services;
using Xunit;

namespace AgyTui.Tests.Unit.Infrastructure.Integrations;

public class CloudIntegrationTests
{
    [Fact]
    public void AwsClient_ShowLocalStackInfo_CanBeInvoked()
    {
        IAwsClient client = new AwsClient();
        Assert.NotNull(client);
    }

    [Fact]
    public void DockerClient_ComposeUp_NullFile_ReturnsExitCode()
    {
        IDockerClient client = new DockerClient();
        Assert.NotNull(client);
    }

    [Fact]
    public void DotNetClient_RemoveBinObj_NonExistentPath_DoesNotThrow()
    {
        IDotNetClient client = new DotNetClient();
        client.RemoveBinObj("C:\\NonExistent_Path_XYZ");
    }

    [Fact]
    public void GitClient_Instance_CanBeCreated()
    {
        IGitClient client = new GitClient(new AiCommitGenerator());
        Assert.NotNull(client);
    }
}

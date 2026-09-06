using AgyTui.Infrastructure.Common;
using AgyTui.Infrastructure.Di;
using Microsoft.Extensions.DependencyInjection;

namespace AgyTui.Tests.Unit.Infrastructure.Common;

public class CommonServicesTests
{
    [Fact]
    public void HttpClientProvider_ReturnsValidHttpClient_AndResolvesFromDI()
    {
        var provider = Bootstrapper.ServiceProvider.GetRequiredService<IHttpClientProvider>();
        Assert.NotNull(provider);
        Assert.NotNull(provider.Client);

        IHttpClientProvider instance = HttpClientProvider.Instance;
        Assert.NotNull(instance.Client);
    }

    [Fact]
    public void ProcessRunner_FindOnPathAndRunCapture_ResolvesFromDI_AndExecutes()
    {
        var runner = Bootstrapper.ServiceProvider.GetRequiredService<IProcessRunner>();
        Assert.NotNull(runner);

        var tool = OperatingSystem.IsWindows() ? "cmd.exe" : "sh";
        var cmdPath = runner.FindOnPath(tool);
        Assert.False(string.IsNullOrEmpty(cmdPath));

        var arg = OperatingSystem.IsWindows() ? "/c echo hello_process_runner" : "-c \"echo hello_process_runner\"";
        var output = runner.RunCapture(tool, arg);
        Assert.Contains("hello_process_runner", output);

        var detailArg = OperatingSystem.IsWindows() ? "/c echo details_test" : "-c \"echo details_test\"";
        var (stdout, stderr, exitCode) = runner.RunCaptureWithDetails(tool, detailArg);
        Assert.Equal(0, exitCode);
        Assert.Contains("details_test", stdout);

        var runArg = OperatingSystem.IsWindows() ? "/c echo run_test" : "-c \"echo run_test\"";
        var runCode = runner.Run(tool, runArg);
        Assert.Equal(0, runCode);
    }

    [Fact]
    public void SystemHelper_FuzzyMatchAndBoldMatch_ResolvesFromDI_AndOperatesCorrectly()
    {
        var helper = Bootstrapper.ServiceProvider.GetRequiredService<ISystemHelper>();
        Assert.NotNull(helper);

        Assert.True(helper.IsFuzzyMatch("Antigravity", "anti"));
        Assert.True(helper.IsFuzzyMatch("Antigravity", "agty"));
        Assert.False(helper.IsFuzzyMatch("Antigravity", "xyz"));

        var bold = helper.BoldFuzzyMatch("Antigravity", "anti");
        Assert.Contains("[bold yellow]", bold);

        var ip = helper.GetPublicIP();
        Assert.NotNull(ip);
    }
}

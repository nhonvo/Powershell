namespace AgyTui.Infrastructure.Integrations;

public abstract class CliToolWrapper
{
    protected readonly string BinaryName;

    protected CliToolWrapper(string binaryName)
    {
        BinaryName = binaryName;
    }

    protected string RunCapture(string args, string? workingDir = null)
    {
        return ProcessRunner.Instance.RunCapture(BinaryName, args, workingDir);
    }

    protected void RunInteractive(IEnumerable<string> args, IDictionary<string, string?>? env = null, string? workingDir = null)
    {
        ProcessRunner.Instance.RunInteractive(BinaryName, args, env, workingDir);
    }
}

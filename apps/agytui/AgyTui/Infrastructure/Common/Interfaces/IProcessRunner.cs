namespace AgyTui.Infrastructure.Common;

public interface IProcessRunner
{
    string? FindOnPath(string exe);
    void RunInteractive(string exe, IEnumerable<string> args, IDictionary<string, string?>? env = null, string? workingDir = null);
    string RunCapture(string exe, string args, string? workingDir = null);
    string RunCapture(string exe, IEnumerable<string> args, string? workingDir = null);
    string RunCapture(string exe, string[] args, string? workingDir = null);
    (string Stdout, string Stderr, int ExitCode) RunCaptureWithDetails(string exe, string args, string? workingDir = null, TimeSpan? timeout = null);
    (string Stdout, string Stderr, int ExitCode) RunCaptureWithDetails(string exe, IEnumerable<string> args, string? workingDir = null, TimeSpan? timeout = null);
    int Run(string exe, string args, string? workingDir = null, TimeSpan? timeout = null);
    int Run(string exe, string[] args, string? workingDir = null);
}

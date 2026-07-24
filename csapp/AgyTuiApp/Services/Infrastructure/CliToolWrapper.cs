using System;
using System.Collections.Generic;
using AgyTui.Helpers;

namespace AgyTui;

public abstract class CliToolWrapper
{
    protected readonly string BinaryName;

    protected CliToolWrapper(string binaryName)
    {
        BinaryName = binaryName;
    }

    protected string RunCapture(string args, string? workingDir = null)
    {
        return ProcessRunner.RunCapture(BinaryName, args, workingDir);
    }

    protected void RunInteractive(IEnumerable<string> args, IDictionary<string, string?>? env = null, string? workingDir = null)
    {
        ProcessRunner.RunInteractive(BinaryName, args, env, workingDir);
    }
}

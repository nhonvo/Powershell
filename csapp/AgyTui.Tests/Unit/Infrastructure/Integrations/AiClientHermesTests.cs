namespace AgyTui.Tests.Unit.Infrastructure.Integrations;

using System.Collections.Generic;
using AgyTui.Infrastructure.Integrations.Ai;
using Xunit;

public class AiClientHermesTests
{
    [Fact]
    public void InvokeHermes_NonDefaultModelArg_PreservesModelFlagAndValue()
    {
        var inputArgs = new[] { "--model", "llama3:latest", "--verbose" };
        var cleaned = AgyAiCore.CleanHermesArguments(inputArgs, AgyAiCore.OllamaDefaultModel);

        Assert.Contains("--model", cleaned);
        Assert.Contains("llama3:latest", cleaned);
        var modelIndex = cleaned.IndexOf("--model");
        Assert.True(modelIndex >= 0);
        Assert.Equal("llama3:latest", cleaned[modelIndex + 1]);
        Assert.Contains("--verbose", cleaned);
    }
}

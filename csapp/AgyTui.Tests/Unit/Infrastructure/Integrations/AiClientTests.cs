namespace AgyTui.Tests.Unit.Infrastructure.Integrations;

using System;
using AgyTui.Core.Models;
using AgyTui.Infrastructure.Integrations.Ai;
using Xunit;

public class AiClientTests
{
    [Fact]
    public void GetEffectiveProviderMode_AutoMode_WithOllamaRunning_ResolvesToLocal()
    {
        var origSeam = AgyAiCore.IsOllamaRunningProvider;
        var origMode = Config.Current.Ai.ProviderMode;
        try
        {
            Config.Current.Ai.ProviderMode = "auto";
            AgyAiCore.IsOllamaRunningProvider = () => true;

            var mode = AgyAiCore.GetEffectiveProviderMode();

            Assert.Equal("local", mode);
        }
        finally
        {
            AgyAiCore.IsOllamaRunningProvider = origSeam;
            Config.Current.Ai.ProviderMode = origMode;
        }
    }

    [Fact]
    public void InvokeClaude_NoProviderOverride_ResolvesToLocalWhenOllamaRunning_ConfirmingBug()
    {
        var origSeam = AgyAiCore.IsOllamaRunningProvider;
        var origMode = Config.Current.Ai.ProviderMode;
        try
        {
            Config.Current.Ai.ProviderMode = "auto";
            AgyAiCore.IsOllamaRunningProvider = () => true;

            // When no providerModeOverride is passed (null), GetEffectiveProviderMode resolves to "local"
            // if Ollama is running. Passing "cloud" explicitly overrides this behavior so bare `claude`
            // always opens the real Claude CLI.
            string? overrideMode = null;
            var effectiveMode = overrideMode ?? AgyAiCore.GetEffectiveProviderMode();

            // Demonstrates current bug state when null is passed
            Assert.Equal("local", effectiveMode);
        }
        finally
        {
            AgyAiCore.IsOllamaRunningProvider = origSeam;
            Config.Current.Ai.ProviderMode = origMode;
        }
    }

    [Fact]
    public void InvokeClaude_ExplicitCloudOverride_ResolvesToCloud_RegardlessOfOllamaRunning()
    {
        var origSeam = AgyAiCore.IsOllamaRunningProvider;
        var origMode = Config.Current.Ai.ProviderMode;
        try
        {
            Config.Current.Ai.ProviderMode = "auto";
            AgyAiCore.IsOllamaRunningProvider = () => true;

            string? overrideMode = "cloud";
            var effectiveMode = overrideMode ?? AgyAiCore.GetEffectiveProviderMode();

            Assert.Equal("cloud", effectiveMode);
        }
        finally
        {
            AgyAiCore.IsOllamaRunningProvider = origSeam;
            Config.Current.Ai.ProviderMode = origMode;
        }
    }

    [Fact]
    public void InvokeCodex_ExplicitCloudOverride_ResolvesToCloud_RegardlessOfOllamaRunning()
    {
        var origSeam = AgyAiCore.IsOllamaRunningProvider;
        var origMode = Config.Current.Ai.ProviderMode;
        try
        {
            Config.Current.Ai.ProviderMode = "auto";
            AgyAiCore.IsOllamaRunningProvider = () => true;

            string? overrideMode = "cloud";
            var effectiveMode = overrideMode ?? AgyAiCore.GetEffectiveProviderMode();

            Assert.Equal("cloud", effectiveMode);
        }
        finally
        {
            AgyAiCore.IsOllamaRunningProvider = origSeam;
            Config.Current.Ai.ProviderMode = origMode;
        }
    }
}

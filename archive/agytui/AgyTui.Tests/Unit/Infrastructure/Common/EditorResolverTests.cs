using AgyTui.Infrastructure.Common;
using Xunit;

namespace AgyTui.Tests.Unit.Infrastructure.Common;

public class EditorResolverTests
{
    [Fact]
    public void Resolve_ReturnsNonEmptyString()
    {
        IEditorResolver resolver = new EditorResolver();
        var editor = resolver.Resolve();
        Assert.False(string.IsNullOrWhiteSpace(editor));
    }

    [Fact]
    public void ResolveEditor_StaticFacade_ReturnsValidEditor()
    {
        IEditorResolver resolver = new EditorResolver();
        var editor = resolver.Resolve();
        Assert.NotNull(editor);
        Assert.True(editor.Length > 0);
    }

    [Fact]
    public void Resolve_ZeroEnvVarsSet_DefaultsToMicroOrFallback()
    {
        // Zero case: VISUAL and EDITOR empty
        var originalVisual = Environment.GetEnvironmentVariable("VISUAL");
        var originalEditor = Environment.GetEnvironmentVariable("EDITOR");

        try
        {
            Environment.SetEnvironmentVariable("VISUAL", "");
            Environment.SetEnvironmentVariable("EDITOR", "");

            IEditorResolver resolver = new EditorResolver();
            var editor = resolver.Resolve();

            Assert.NotNull(editor);
            Assert.NotEmpty(editor);
        }
        finally
        {
            Environment.SetEnvironmentVariable("VISUAL", originalVisual);
            Environment.SetEnvironmentVariable("EDITOR", originalEditor);
        }
    }

    [Fact]
    public void Resolve_NotepadSetInEnv_IgnoresNotepadAndResolvesTerminalEditor()
    {
        // Failure/Edge case: Notepad set as EDITOR (should be ignored for TUI environment)
        var originalEditor = Environment.GetEnvironmentVariable("EDITOR");

        try
        {
            Environment.SetEnvironmentVariable("EDITOR", "notepad");

            IEditorResolver resolver = new EditorResolver();
            var editor = resolver.Resolve();

            Assert.NotEqual("notepad", editor, StringComparer.OrdinalIgnoreCase);
        }
        finally
        {
            Environment.SetEnvironmentVariable("EDITOR", originalEditor);
        }
    }
}

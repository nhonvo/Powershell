namespace AgyTui.Infrastructure.Common;

public class EditorResolver : IEditorResolver
{
    public string Resolve() => ResolveEditor();

    public static string ResolveEditor()
    {
        var visual = Environment.GetEnvironmentVariable("VISUAL");
        if (!string.IsNullOrWhiteSpace(visual) && !visual.Equals("notepad", StringComparison.OrdinalIgnoreCase)) return visual;

        var editor = Environment.GetEnvironmentVariable("EDITOR");
        if (!string.IsNullOrWhiteSpace(editor) && !editor.Equals("notepad", StringComparison.OrdinalIgnoreCase)) return editor;

        (string coreEditor, _, int exitCode) = ProcessRunner.RunCaptureWithDetails("git", "config core.editor");
        if (exitCode == 0 && !string.IsNullOrWhiteSpace(coreEditor) && !coreEditor.Trim().Equals("notepad", StringComparison.OrdinalIgnoreCase))
            return coreEditor.Trim();

        foreach (var termEditor in new[] { "micro", "nvim", "vim", "nano" })
        {
            if (!string.IsNullOrEmpty(ProcessRunner.FindOnPath(termEditor)))
                return termEditor;
        }

        return "micro";
    }
}

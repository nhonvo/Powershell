using System.Runtime.InteropServices;

namespace AgyTui.Infrastructure.Common;

public static class EditorResolver
{
    public static string Resolve()
    {
        var visual = Environment.GetEnvironmentVariable("VISUAL");
        if (!string.IsNullOrWhiteSpace(visual)) return visual;

        var editor = Environment.GetEnvironmentVariable("EDITOR");
        if (!string.IsNullOrWhiteSpace(editor)) return editor;

        (string coreEditor, _, int exitCode) = ProcessRunner.RunCaptureWithDetails("git", "config core.editor");
        if (exitCode == 0 && !string.IsNullOrWhiteSpace(coreEditor)) return coreEditor.Trim();

        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "notepad" : "nano";
    }
}

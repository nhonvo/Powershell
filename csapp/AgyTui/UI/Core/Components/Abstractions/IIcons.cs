using AgyTui.Domain.LearnContext;

namespace AgyTui.UI.Core.Components.Abstractions;

public interface IIcons
{
    bool IsUtf8Supported { get; }
    bool UseNerdFonts { get; set; }
    int GetGlyphDisplayWidth(string glyph);
    int GetStringDisplayWidth(string text);
    int GetCodePointDisplayWidth(int codePoint);
    string GetFileIcon(string ext);
    string FolderClosed { get; }
    string FolderOpen { get; }
    string GetCategoryIcon(string categoryLabel);
    string GetCategoryHotkey(string categoryLabel);
    string GetCommandIcon(string alias, string category);
    string GetStatusIcon(string status);
    string GetGitGutter(string changeType);
    string GetProviderIcon(string provider);
    string GetModelIcon(string family);
    string GetSubjectIcon(string subject);
    string GetMasteryIcon(string mastery);
    string GetMasteryIcon(SrState sr);
}

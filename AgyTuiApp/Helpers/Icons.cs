using System;

namespace AgyTui;

public static class Icons
{
    public static bool UseNerdFonts { get; set; } = 
        string.Equals(Environment.GetEnvironmentVariable("AGY_NERD_FONTS"), "true", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(Environment.GetEnvironmentVariable("POSH_THEME_NERD_FONTS"), "true", StringComparison.OrdinalIgnoreCase);

    public static string GetFileIcon(string ext)
    {
        ext = ext.ToLowerInvariant();
        if (UseNerdFonts)
        {
            return ext switch
            {
                ".cs" => "󰌛",
                ".json" => "",
                ".md" => "",
                ".txt" => "󰈙",
                ".ps1" or ".sh" => "󱆃",
                ".yaml" or ".yml" => "",
                ".csproj" or ".sln" => "󰪚",
                ".ts" or ".tsx" or ".js" => "",
                ".sql" => "",
                ".py" => "",
                _ => "󰈙"
            };
        }
        else
        {
            return ext switch
            {
                ".cs" => "⚙",
                ".json" => "📋",
                ".md" => "📝",
                ".txt" => "📄",
                ".ps1" or ".sh" => "⚡",
                ".yaml" or ".yml" => "🔧",
                ".csproj" or ".sln" => "🏗",
                ".ts" or ".tsx" or ".js" => "🟨",
                ".sql" => "🗄",
                ".py" => "🐍",
                _ => "📄"
            };
        }
    }

    public static string FolderClosed => UseNerdFonts ? "󰉋" : "📁";
    public static string FolderOpen => UseNerdFonts ? "󰉓" : "📂";

    public static string GetStatusIcon(string status)
    {
        status = status.ToLowerInvariant();
        return status switch
        {
            "running" or "online" or "success" or "active" => "🟢",
            "error" or "failed" or "critical" => "🔴",
            "offline" or "stopped" or "inactive" => "⚫",
            "warning" or "pending" => "🟡",
            "dirty" or "unsaved" => "●",
            _ => "⚫"
        };
    }

    public static string GetGitGutter(string changeType)
    {
        return changeType.ToLowerInvariant() switch
        {
            "added" => "+",
            "modified" => "~",
            "removed" => "-",
            _ => " "
        };
    }

    public static string GetProviderIcon(string provider)
    {
        provider = provider.ToLowerInvariant();
        if (UseNerdFonts)
        {
            return provider switch
            {
                "claude" => "󰚩",
                "codex" => "󰚩",
                "ollama" => "🦙",
                "openclaw" => "󰚩",
                "hermes" => "󰚩",
                _ => "󰚩"
            };
        }
        else
        {
            return provider switch
            {
                "claude" => "✳",
                "codex" => "▪",
                "ollama" => "🦙",
                "openclaw" => "🐾",
                "hermes" => "☤",
                _ => "🧠"
            };
        }
    }

    public static string GetModelIcon(string family)
    {
        family = family.ToLowerInvariant();
        if (family.Contains("llama")) return "🦙";
        if (family.Contains("mistral")) return "🌬";
        if (family.Contains("qwen")) return "🐈";
        if (family.Contains("gemma")) return "💎";
        return "🧠";
    }

    public static string GetSubjectIcon(string subject)
    {
        subject = subject.ToLowerInvariant();
        if (UseNerdFonts)
        {
            return subject switch
            {
                "vocab" or "en" => "󰗊",
                "kana" or "jp" => "あ",
                "kanji" => "漢",
                "jlpt" => "🎓",
                "algo" or "dsa" => "🧮",
                "weak" or "star" => "⭐",
                "streak" => "🔥",
                _ => "📚"
            };
        }
        else
        {
            return subject switch
            {
                "vocab" or "en" => "🔤",
                "kana" or "jp" => "あ",
                "kanji" => "漢",
                "jlpt" => "🎓",
                "algo" or "dsa" => "🧮",
                "weak" or "star" => "⭐",
                "streak" => "🔥",
                _ => "📚"
            };
        }
    }

    public static string GetMasteryIcon(string mastery)
    {
        return mastery.ToLowerInvariant() switch
        {
            "new" => "🌱",
            "learning" => "🌿",
            "mature" => "🌳",
            _ => "🌱"
        };
    }
}

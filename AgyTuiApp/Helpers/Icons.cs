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

    public static string GetCategoryIcon(string categoryLabel)
    {
        categoryLabel = categoryLabel.ToLowerInvariant();
        if (categoryLabel.Contains("workspace")) return "📁";
        if (categoryLabel.Contains("ai agent") || categoryLabel.Contains("ollama")) return "🤖";
        if (categoryLabel.Contains("account")) return "👤";
        if (categoryLabel.Contains("docker") || categoryLabel.Contains("database")) return "🐳";
        if (categoryLabel.Contains("system") || categoryLabel.Contains("network")) return "🌐";
        if (categoryLabel.Contains("learn") || categoryLabel.Contains("study")) return "📚";
        if (categoryLabel.Contains("track") || categoryLabel.Contains("progress")) return "📈";
        if (categoryLabel.Contains("obsidian") || categoryLabel.Contains("resource")) return "💎";
        if (categoryLabel.Contains("theme") || categoryLabel.Contains("setting")) return "🎨";
        return "📂";
    }

    public static string GetCommandIcon(string alias, string category)
    {
        alias = alias.ToLowerInvariant();
        category = category.ToLowerInvariant();

        if (category.Contains("ai agent") || category.Contains("ollama"))
        {
            var provider = alias.Split('-')[0];
            if (alias.StartsWith("deck")) return "🧠";
            if (alias.StartsWith("ollama")) return "🦙";
            return GetProviderIcon(provider);
        }

        if (category.Contains("learn") || category.Contains("study"))
        {
            return GetSubjectIcon(alias);
        }

        if (UseNerdFonts)
        {
            return alias switch
            {
                "proj" => "󰉋",
                "ide" => "󰨞",
                "ide-diff" => "󰊢",
                "ide-search" => "󰍉",
                "dbld" => "⚙",
                "dtst" => "󰙨",
                "clean-build" => "󰃢",
                "add-migration" => "󰆼",
                "update-db" => "󰆼",
                "scaffold" => "🏗",
                "gs" => "󰊢",
                "gcmt" => "💬",
                "git-undo" => "↩",
                "nexus" or "repo-graph" or "nexus-stats" => "🕸",

                "agyswitch" or "account-tree" => "👤",
                "agyquota" or "quota-chart" or "live-dashboard" => "📊",
                "autoswitch" => "⚡",

                "docker-health" or "dkcl" or "dcup" or "dcdown" => "🐳",
                "aws-local" => "☁",
                "db-tui" => "🗄",

                "tailscale-status" => "🔒",
                "ssh-qr" or "ssh-info" => "🔑",
                "disk" => "💾",
                "public-ip" => "🌐",
                "kill-port" => "🚫",

                "session" => "⏱",
                "stats" or "progress" => "📈",
                "goals" => "🎯",
                "streak" => "🔥",
                "due" or "weak" => "⭐",

                "obsidian" or "obs-graph" => "💎",
                "refresh" => "🔄",
                "add-resource" => "📌",

                "cc" or "help" => "🛸",
                "mobile-setup" => "📱",
                "theme" => "🎨",
                "ui-mode" or "density" => "🖥",

                _ => GetFileIcon(".txt")
            };
        }
        else
        {
            return alias switch
            {
                "proj" => "📁",
                "ide" => "💻",
                "ide-diff" => "🔀",
                "ide-search" => "🔍",
                "dbld" => "⚙",
                "dtst" => "🧪",
                "clean-build" => "🧹",
                "add-migration" or "update-db" => "🗄",
                "scaffold" => "🏗",
                "gs" => "🌿",
                "gcmt" => "💬",
                "git-undo" => "↩",
                "nexus" or "repo-graph" or "nexus-stats" => "🕸",

                "agyswitch" or "account-tree" => "👤",
                "agyquota" or "quota-chart" or "live-dashboard" => "📊",
                "autoswitch" => "⚡",

                "docker-health" or "dkcl" or "dcup" or "dcdown" => "🐳",
                "aws-local" => "☁",
                "db-tui" => "🗄",

                "tailscale-status" => "🔒",
                "ssh-qr" => "📱",
                "ssh-info" => "🔑",
                "disk" => "💾",
                "public-ip" => "🌐",
                "kill-port" => "🚫",

                "session" => "⏱",
                "stats" or "progress" => "📈",
                "goals" => "🎯",
                "streak" => "🔥",
                "due" or "weak" => "⭐",

                "obsidian" or "obs-graph" => "💎",
                "refresh" => "🔄",
                "add-resource" => "📌",

                "cc" or "help" => "🛸",
                "mobile-setup" => "📱",
                "theme" => "🎨",
                "ui-mode" or "density" => "🖥",

                _ => GetFileIcon(".txt")
            };
        }
    }

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

using System;

namespace AgyTui;

public static class Icons
{
    public static bool IsUtf8Supported
    {
        get
        {
            try
            {
                return Console.OutputEncoding.CodePage == 65001;
            }
            catch
            {
                return false;
            }
        }
    }

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
        else if (!IsUtf8Supported)
        {
            return ext switch
            {
                ".cs" => "[CS]",
                ".json" => "[JSON]",
                ".md" => "[MD]",
                ".txt" => "[TXT]",
                ".ps1" or ".sh" => "[SH]",
                ".yaml" or ".yml" => "[YML]",
                ".csproj" or ".sln" => "[PROJ]",
                ".ts" or ".tsx" or ".js" => "[JS]",
                ".sql" => "[SQL]",
                ".py" => "[PY]",
                _ => "[FILE]"
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

    public static string FolderClosed => UseNerdFonts ? "󰉋" : (IsUtf8Supported ? "📁" : "[+]");
    public static string FolderOpen => UseNerdFonts ? "󰉓" : (IsUtf8Supported ? "📂" : "[-]");

    private record CategoryMeta(string Keyword, string NerdIcon, string Utf8Icon, string AsciiIcon, string Hotkey);

    private static readonly CategoryMeta[] Categories = new CategoryMeta[]
    {
        new("workspace", "󰉋", "📁", "[Dev]", "cnav"),
        new("ai agent", "󰚩", "🤖", "[AI]", "cai"),
        new("ollama", "󰚩", "🤖", "[AI]", "cai"),
        new("account", "👤", "👤", "[ACC]", "agyswitch"),
        new("docker", "🐳", "🐳", "[DKR]", "cdk"),
        new("database", "🐳", "🐳", "[DKR]", "cdk"),
        new("system", "🌐", "🌐", "[SYS]", "csys"),
        new("network", "🌐", "🌐", "[SYS]", "csys"),
        new("learn", "📚", "📚", "[LRN]", "learn"),
        new("study", "📚", "📚", "[LRN]", "learn"),
        new("track", "📈", "📈", "[TRK]", "track"),
        new("progress", "📈", "📈", "[TRK]", "track"),
        new("obsidian", "💎", "💎", "[OBS]", "obsidian"),
        new("resource", "💎", "💎", "[OBS]", "obsidian"),
        new("appearance", "🎨", "🎨", "[UI]", "theme"),
        new("layout", "🎨", "🎨", "[UI]", "theme"),
        new("help", "🛸", "🛸", "[AGY]", "help"),
        new("docs", "🛸", "🛸", "[AGY]", "help"),
    };

    public static string GetCategoryIcon(string categoryLabel)
    {
        var lower = categoryLabel.ToLowerInvariant();
        var match = Array.Find(Categories, c => lower.Contains(c.Keyword));
        if (match != null)
        {
            return UseNerdFonts ? match.NerdIcon : (IsUtf8Supported ? match.Utf8Icon : match.AsciiIcon);
        }
        return UseNerdFonts ? "󰉋" : (IsUtf8Supported ? "📂" : "[+]");
    }

    public static string GetCategoryHotkey(string categoryLabel)
    {
        var lower = categoryLabel.ToLowerInvariant();
        var match = Array.Find(Categories, c => lower.Contains(c.Keyword));
        return match?.Hotkey ?? "";
    }

    public static string GetCommandIcon(string alias, string category)
    {
        alias = alias.ToLowerInvariant();
        category = category.ToLowerInvariant();

        var icon = GetAliasIcon(alias);
        if (icon != null) return icon;

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

        return GetFileIcon(".txt");
    }

    private static string? GetAliasIcon(string alias)
    {
        if (UseNerdFonts)
        {
            return alias switch
            {
                "proj" => "󰉋",
                "ide" => "󰨞",
                "ide-diff" => "󰊢",
                "ide-search" => "󰍉",
                "dbld" or "drestore" or "dpublish" or "dwatch" or "rebuild" => "⚙",
                "dtst" => "󰙨",
                "clean-build" => "󰃢",
                "add-migration" or "update-db" => "󰆼",
                "scaffold" => "🏗",
                "gs" or "gbr" or "glog" or "gpull" or "gpush" => "󰊢",
                "gcmt" => "💬",
                "git-undo" => "↩",
                "nexus" or "repo-graph" or "nexus-stats" => "🕸",
                "agyswitch" or "account-tree" => "👤",
                "agyquota" or "quota-chart" or "live-dashboard" => "📊",
                "autoswitch" => "⚡",
                "docker-health" or "dkcl" or "dcup" or "dcdown" or "dimg" or "dlogs" or "dkrmac" or "dkstac" => "🐳",
                "aws-local" or "aws-whoami" or "aws-s3" or "aws-sqs" or "aws-ssm" or "aws-sns" or "aws-dynamodb" or "aws-lambda" => "☁",
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
                "hotkeys" or "hotkey" => "⌨",
                _ => null
            };
        }

        return alias switch
        {
            "proj" => "📁",
            "ide" => "💻",
            "ide-diff" => "🔀",
            "ide-search" => "🔍",
            "dbld" => "⚙",
            "dtst" => "🧪",
            "drestore" => "📦",
            "dpublish" => "🚀",
            "dwatch" => "👀",
            "clean-build" => "🧹",
            "add-migration" or "update-db" => "🗄",
            "scaffold" => "🏗",
            "gs" or "gbr" => "🌿",
            "gcmt" => "💬",
            "glog" => "📜",
            "gpull" => "⬇",
            "gpush" => "⬆",
            "git-undo" => "↩",
            "nexus" or "repo-graph" or "nexus-stats" => "🕸",
            "agyswitch" or "account-tree" => "👤",
            "agyquota" or "quota-chart" or "live-dashboard" => "📊",
            "autoswitch" => "⚡",
            "docker-health" or "dkcl" or "dcup" or "dcdown" => "🐳",
            "dimg" => "🖼",
            "dlogs" => "📄",
            "aws-local" => "☁",
            "aws-whoami" => "👤",
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
            "hotkeys" or "hotkey" => "⌨",
            _ => null
        };
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
            "review" => "🌿",
            "mastered" => "🌳",
            "mature" => "🌳",
            _ => "🌱"
        };
    }

    public static string GetMasteryIcon(SrState sr)
    {
        if (sr.Status.Equals("mastered", StringComparison.OrdinalIgnoreCase) || sr.IntervalDays >= 21) return "🌳";
        if (sr.Status.Equals("review", StringComparison.OrdinalIgnoreCase) || sr.IntervalDays >= 3) return "🌿";
        return "🌱";
    }
}

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

    public static string GetCategoryIcon(string categoryLabel)
    {
        categoryLabel = categoryLabel.ToLowerInvariant();
        if (UseNerdFonts)
        {
            if (categoryLabel.Contains("workspace")) return "󰉋";
            if (categoryLabel.Contains("ai agent") || categoryLabel.Contains("ollama")) return "󰚩";
            if (categoryLabel.Contains("account")) return "👤";
            if (categoryLabel.Contains("docker") || categoryLabel.Contains("database")) return "🐳";
            if (categoryLabel.Contains("system") || categoryLabel.Contains("network")) return "🌐";
            if (categoryLabel.Contains("learn") || categoryLabel.Contains("study")) return "📚";
            if (categoryLabel.Contains("track") || categoryLabel.Contains("progress")) return "📈";
            if (categoryLabel.Contains("obsidian") || categoryLabel.Contains("resource")) return "💎";
            if (categoryLabel.Contains("appearance") || categoryLabel.Contains("layout")) return "🎨";
            if (categoryLabel.Contains("help") || categoryLabel.Contains("docs")) return "🛸";
            return "󰉋";
        }
        else if (!IsUtf8Supported)
        {
            if (categoryLabel.Contains("workspace")) return "[Dev]";
            if (categoryLabel.Contains("ai agent") || categoryLabel.Contains("ollama")) return "[AI]";
            if (categoryLabel.Contains("account")) return "[ACC]";
            if (categoryLabel.Contains("docker") || categoryLabel.Contains("database")) return "[DKR]";
            if (categoryLabel.Contains("system") || categoryLabel.Contains("network")) return "[SYS]";
            if (categoryLabel.Contains("learn") || categoryLabel.Contains("study")) return "[LRN]";
            if (categoryLabel.Contains("track") || categoryLabel.Contains("progress")) return "[TRK]";
            if (categoryLabel.Contains("obsidian") || categoryLabel.Contains("resource")) return "[OBS]";
            if (categoryLabel.Contains("appearance") || categoryLabel.Contains("layout")) return "[UI]";
            if (categoryLabel.Contains("help") || categoryLabel.Contains("docs")) return "[AGY]";
            return "[+]";
        }
        else
        {
            if (categoryLabel.Contains("workspace")) return "📁";
            if (categoryLabel.Contains("ai agent") || categoryLabel.Contains("ollama")) return "🤖";
            if (categoryLabel.Contains("account")) return "👤";
            if (categoryLabel.Contains("docker") || categoryLabel.Contains("database")) return "🐳";
            if (categoryLabel.Contains("system") || categoryLabel.Contains("network")) return "🌐";
            if (categoryLabel.Contains("learn") || categoryLabel.Contains("study")) return "📚";
            if (categoryLabel.Contains("track") || categoryLabel.Contains("progress")) return "📈";
            if (categoryLabel.Contains("obsidian") || categoryLabel.Contains("resource")) return "💎";
            if (categoryLabel.Contains("appearance") || categoryLabel.Contains("layout")) return "🎨";
            if (categoryLabel.Contains("help") || categoryLabel.Contains("docs")) return "🛸";
            if (categoryLabel.Contains("theme") || categoryLabel.Contains("setting")) return "🎨";
            return "📂";
        }
    }

    public static string GetCategoryHotkey(string categoryLabel)
    {
        categoryLabel = categoryLabel.ToLowerInvariant();
        if (categoryLabel.Contains("workspace")) return "cnav";
        if (categoryLabel.Contains("ai agent") || categoryLabel.Contains("ollama")) return "cai";
        if (categoryLabel.Contains("account")) return "agyswitch";
        if (categoryLabel.Contains("docker") || categoryLabel.Contains("database")) return "cdk";
        if (categoryLabel.Contains("system") || categoryLabel.Contains("network")) return "csys";
        if (categoryLabel.Contains("learn") || categoryLabel.Contains("study")) return "learn";
        if (categoryLabel.Contains("track") || categoryLabel.Contains("progress")) return "stats";
        if (categoryLabel.Contains("obsidian") || categoryLabel.Contains("resource")) return "obsidian";
        if (categoryLabel.Contains("appearance") || categoryLabel.Contains("layout")) return "theme";
        if (categoryLabel.Contains("help") || categoryLabel.Contains("docs")) return "help";
        if (categoryLabel.Contains("theme") || categoryLabel.Contains("setting")) return "theme";
        return "";
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
                "dbld" or "drestore" or "dpublish" or "dwatch" or "rebuild" => "⚙",
                "dtst" => "󰙨",
                "clean-build" => "󰃢",
                "add-migration" => "󰆼",
                "update-db" => "󰆼",
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
                "drestore" => "📦",
                "dpublish" => "🚀",
                "dwatch" => "👀",
                "clean-build" => "🧹",
                "add-migration" or "update-db" => "🗄",
                "scaffold" => "🏗",
                "gs" => "🌿",
                "gbr" => "🌿",
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

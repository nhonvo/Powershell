namespace AgyTui.Infrastructure.Common;

public static class AppPaths
{
    private static string? _projectRoot;
    private static string? _repoRoot;

    public static string ProjectRoot
    {
        get
        {
            if (_projectRoot != null) return _projectRoot;
            try
            {
                var dir = AppContext.BaseDirectory;
                var probe = Path.GetFullPath(Path.Combine(dir, "..", "..", ".."));
                if (File.Exists(Path.Combine(probe, "AgyTui.csproj")))
                {
                    _projectRoot = probe;
                    return _projectRoot;
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log($"[AppPaths] Probe failed: {ex.Message}", "DEBUG");
            }

            var pwd = Directory.GetCurrentDirectory();
            if (File.Exists(Path.Combine(pwd, "AgyTui.csproj")))
            {
                _projectRoot = pwd;
                return _projectRoot;
            }

            _projectRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Powershell", "csapp", "AgyTui");
            return _projectRoot;
        }
    }

    public static string RepoRoot
    {
        get
        {
            if (_repoRoot != null) return _repoRoot;
            var envRoot = Environment.GetEnvironmentVariable("PROFILE_REPO_ROOT");
            if (!string.IsNullOrEmpty(envRoot) && (File.Exists(Path.Combine(envRoot, "csapp", "AgyTui", "profile.config.json")) || File.Exists(Path.Combine(envRoot, "csapp", "profile.config.json"))))
            {
                _repoRoot = envRoot;
                return _repoRoot;
            }

            var startDir = AppContext.BaseDirectory;
            if (string.IsNullOrEmpty(startDir) && !string.IsNullOrEmpty(Environment.ProcessPath))
            {
                startDir = Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;
            }

            var curr = new DirectoryInfo(startDir);
            while (curr != null)
            {
                if (File.Exists(Path.Combine(curr.FullName, "csapp", "AgyTui", "profile.config.json")) || File.Exists(Path.Combine(curr.FullName, "csapp", "profile.config.json")))
                {
                    _repoRoot = curr.FullName;
                    return _repoRoot;
                }
                if (File.Exists(Path.Combine(curr.FullName, "profile.config.json")))
                {
                    _repoRoot = string.Equals(curr.Name, "csapp", StringComparison.OrdinalIgnoreCase) && curr.Parent != null
                        ? curr.Parent.FullName
                        : curr.FullName;
                    return _repoRoot;
                }
                curr = curr.Parent;
            }

            _repoRoot = Path.GetFullPath(Path.Combine(ProjectRoot, "..", ".."));
            return _repoRoot;
        }
    }

    public static string ConfigFile
    {
        get
        {
            var envOverride = Environment.GetEnvironmentVariable("PROFILE_CONFIG_PATH");
            if (!string.IsNullOrEmpty(envOverride)) return envOverride;

            var root = RepoRoot;
            if (root.EndsWith(Path.Combine("csapp", "AgyTui"), StringComparison.OrdinalIgnoreCase))
            {
                return Path.Combine(root, "profile.config.json");
            }
            if (root.EndsWith("csapp", StringComparison.OrdinalIgnoreCase))
            {
                return Path.Combine(root, "AgyTui", "profile.config.json");
            }

            var agyTuiCfg = Path.Combine(root, "csapp", "AgyTui", "profile.config.json");
            if (File.Exists(agyTuiCfg)) return agyTuiCfg;

            var csappCfg = Path.Combine(root, "csapp", "profile.config.json");
            if (File.Exists(csappCfg)) return csappCfg;

            var dir = Path.GetDirectoryName(agyTuiCfg);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            return agyTuiCfg;
        }
    }

    public static string UserProfileDir => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    public static string LocalAppDataDir => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

    public static string LogsDir
    {
        get
        {
            var dir = Path.Combine(ProjectRoot, "logs");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public static string DataDir
    {
        get
        {
            var dir = Path.Combine(ProjectRoot, "data");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public static string LearnDir => Path.Combine(DataDir, "learn");
    public static string ResourcesDir => Path.Combine(DataDir, "resources");
    public static string SkillsDir => Path.Combine(DataDir, "skills");

    public static string CacheDir
    {
        get
        {
            var dir = Path.Combine(DataDir, "cache");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public static string GeminiHome
    {
        get
        {
            var envGemini = Environment.GetEnvironmentVariable("GEMINI_HOME");
            if (!string.IsNullOrEmpty(envGemini) && Directory.Exists(envGemini)) return envGemini;
            var envAgy = Environment.GetEnvironmentVariable("AGY_HOME");
            if (!string.IsNullOrEmpty(envAgy) && Directory.Exists(envAgy)) return envAgy;

            var userProfileGemini = Path.Combine(UserProfileDir, ".gemini");
            if (Directory.Exists(userProfileGemini)) return userProfileGemini;

            var projectGemini = Path.Combine(DataDir, ".gemini");
            Directory.CreateDirectory(projectGemini);
            return projectGemini;
        }
    }

    public static string OllamaDataDir
    {
        get
        {
            var dir = Path.Combine(DataDir, "ollama");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public static string DeckDataDir
    {
        get
        {
            var dir = Path.Combine(DataDir, "deck");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public static string UserSshKeysFile => Path.Combine(UserProfileDir, ".ssh", "authorized_keys");
    public static string DefaultLearningVaultDir => Path.Combine(UserProfileDir, "project", "learning");
    public static string DefaultDesktopProjectDir => Path.Combine(UserProfileDir, "Desktop", "project");
}

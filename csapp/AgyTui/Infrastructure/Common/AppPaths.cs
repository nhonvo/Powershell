namespace AgyTui.Infrastructure.Common;

public static class AppPaths
{
    private static string? _projectRoot;

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
            catch { }

            var pwd = Directory.GetCurrentDirectory();
            if (File.Exists(Path.Combine(pwd, "AgyTui.csproj")))
            {
                _projectRoot = pwd;
                return _projectRoot;
            }

            _projectRoot = @"C:\Users\TruongNhon\Documents\Powershell\csapp\AgyTui";
            return _projectRoot;
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

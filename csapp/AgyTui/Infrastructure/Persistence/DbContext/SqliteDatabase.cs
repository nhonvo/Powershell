using AgyTui.Infrastructure.Configuration;
using AgyTui.Infrastructure.Di;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace AgyTui.Infrastructure.Persistence.DbContext;

public class SqliteDatabase : ISqliteDatabase
{
    private readonly Func<SqliteMigrationEngine> _migrationEngineFactory;

    static SqliteDatabase()
    {
        try
        {
            var asmDir = Path.GetDirectoryName(typeof(SqliteDatabase).Assembly.Location);
            var candidates = new[]
            {
                Path.Combine(asmDir ?? "", "e_sqlite3.dll"),
                Path.Combine(asmDir ?? "", "runtimes", "win-x64", "native", "e_sqlite3.dll"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "e_sqlite3.dll"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "runtimes", "win-x64", "native", "e_sqlite3.dll")
            };
            foreach (var path in candidates)
            {
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    try
                    {
                        System.Runtime.InteropServices.NativeLibrary.Load(path);
                        break;
                    }
                    catch { }
                }
            }
        }
        catch { }

        try
        {
            System.Runtime.InteropServices.NativeLibrary.SetDllImportResolver(
                typeof(Microsoft.Data.Sqlite.SqliteConnection).Assembly,
                (libraryName, assembly, searchPath) =>
                {
                    if (libraryName.Equals("e_sqlite3", StringComparison.OrdinalIgnoreCase) ||
                        libraryName.Equals("e_sqlite3.dll", StringComparison.OrdinalIgnoreCase))
                    {
                        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                        var asmDir = Path.GetDirectoryName(typeof(SqliteDatabase).Assembly.Location) ?? baseDir;
                        var candidates = new[]
                        {
                            Path.Combine(asmDir, "e_sqlite3.dll"),
                            Path.Combine(asmDir, "runtimes", "win-x64", "native", "e_sqlite3.dll"),
                            Path.Combine(baseDir, "e_sqlite3.dll"),
                            Path.Combine(baseDir, "runtimes", "win-x64", "native", "e_sqlite3.dll")
                        };
                        foreach (var path in candidates)
                        {
                            if (File.Exists(path) && System.Runtime.InteropServices.NativeLibrary.TryLoad(path, out var handle))
                            {
                                return handle;
                            }
                        }
                    }
                    return IntPtr.Zero;
                });
        }
        catch { }

        try
        {
            SQLitePCL.Batteries_V2.Init();
        }
        catch
        {
            try
            {
                SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_e_sqlite3());
            }
            catch { }
        }
    }

    public SqliteDatabase(Func<SqliteMigrationEngine>? migrationEngineFactory = null)
    {
        _migrationEngineFactory = migrationEngineFactory ?? (() => new SqliteMigrationEngine(this));
    }

    public virtual string DbPath => Path.Combine(AppPaths.DataDir, EnvironmentProvider.DatabaseFileName);

    public SqliteConnection CreateConnection()
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = DbPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        };
        var conn = new SqliteConnection(builder.ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL;";
        cmd.ExecuteNonQuery();
        return conn;
    }

    public void InitializeDatabase()
    {
        _migrationEngineFactory().ApplyMigrations();
    }
}


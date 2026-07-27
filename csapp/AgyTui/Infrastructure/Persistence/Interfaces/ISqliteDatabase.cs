namespace AgyTui.Infrastructure.Persistence.Interfaces;

using Microsoft.Data.Sqlite;

public interface ISqliteDatabase
{
    string DbPath { get; }
    SqliteConnection CreateConnection();
    void InitializeDatabase();
}

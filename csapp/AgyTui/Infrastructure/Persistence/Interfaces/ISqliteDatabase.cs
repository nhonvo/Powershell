
using Microsoft.Data.Sqlite;

namespace AgyTui.Infrastructure.Persistence.Interfaces;

public interface ISqliteDatabase
{
    string DbPath { get; }
    SqliteConnection CreateConnection();
    void InitializeDatabase();
}

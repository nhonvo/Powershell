using AgyTui.Infrastructure.Persistence;

namespace AgyTui.Tests.Unit.UI.Screens.Database;

public class DatabaseScreenTests
{
    [Fact]
    public void SqliteMigrationEngine_StaticType_Exists()
    {
        Assert.NotNull(typeof(SqliteMigrationEngine));
    }
}

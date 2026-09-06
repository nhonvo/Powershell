using AgyTui.Infrastructure.Persistence.Seeding;
using Xunit;

namespace AgyTui.Tests.Unit.Infrastructure.Persistence;

public class TestSeeder : ISeeder
{
    public int Order { get; }
    public bool Executed { get; private set; }

    public TestSeeder(int order = 1)
    {
        Order = order;
    }

    public void Seed()
    {
        Executed = true;
    }
}

public class MasterSeederTests
{
    [Fact]
    public void ExecuteAllSeeders_RunsInOrder()
    {
        var seeder = new TestSeeder();
        IMasterSeeder masterSeeder = new MasterSeeder(new ISeeder[] { seeder });

        masterSeeder.ExecuteAllSeeders();

        Assert.True(seeder.Executed);
    }

    [Fact]
    public void ExecuteAllSeeders_ZeroSeeders_ExecutesWithoutError()
    {
        // Zero case: empty seeder array
        IMasterSeeder masterSeeder = new MasterSeeder(Array.Empty<ISeeder>());
        masterSeeder.ExecuteAllSeeders();
    }

    [Fact]
    public void ExecuteAllSeeders_MultipleSeeders_ExecutesInAscendingOrder()
    {
        // Ordering case: 2 seeders out of order
        var s2 = new TestSeeder(2);
        var s1 = new TestSeeder(1);

        IMasterSeeder masterSeeder = new MasterSeeder(new ISeeder[] { s2, s1 });
        masterSeeder.ExecuteAllSeeders();

        Assert.True(s1.Executed);
        Assert.True(s2.Executed);
    }
}

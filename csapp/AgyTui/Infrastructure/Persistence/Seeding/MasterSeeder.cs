namespace AgyTui.Infrastructure.Persistence.Seeding;

public class MasterSeeder : IMasterSeeder
{
    private readonly IEnumerable<ISeeder> _seeders;

    public MasterSeeder(IEnumerable<ISeeder> seeders)
    {
        _seeders = seeders.OrderBy(s => s.Order);
    }

    public void ExecuteAllSeeders()
    {
        foreach (var seeder in _seeders)
        {
            try
            {
                seeder.Seed();
            }
            catch (Exception ex)
            {
                LogHelper.Log($"[MasterSeeder] Seeder '{seeder.GetType().Name}' failed: {ex.Message}", "WARN");
            }
        }
    }
}

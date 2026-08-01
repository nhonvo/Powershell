namespace AgyTui.Infrastructure.Persistence.Seeding;

public interface ISeeder
{
    int Order { get; }
    void Seed();
}

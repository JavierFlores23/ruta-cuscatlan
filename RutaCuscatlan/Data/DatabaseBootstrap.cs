using Microsoft.EntityFrameworkCore;

namespace RutaCuscatlan.Data;

public static class DatabaseBootstrap
{
    public static async Task InitializeAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        await db.Database.MigrateAsync(cancellationToken);
        DbSeeder.Seed(db);
    }
}

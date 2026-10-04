using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Ehr.Infrastructure.Persistence;

/// <summary>Design-time factory for <c>dotnet ef</c> (migrations, database update). Uses env or a dev placeholder only.</summary>
public sealed class EhrDbContextFactory : IDesignTimeDbContextFactory<EhrDbContext>
{
    public EhrDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=ehr_db;Username=postgres;Password=CHANGE_ME";

        var options = new DbContextOptionsBuilder<EhrDbContext>()
            .UseNpgsql(
                connectionString,
                b => b.MigrationsHistoryTable("__ef_migrations_history", "public"))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new EhrDbContext(options);
    }
}

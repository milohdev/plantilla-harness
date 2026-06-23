using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Logistics.Infrastructure.Persistence;

/// <summary>
/// Factory de tiempo de diseño para que `dotnet ef migrations` pueda construir
/// el DbContext sin levantar la aplicación.
/// </summary>
public sealed class LogisticsDbContextFactory : IDesignTimeDbContextFactory<LogisticsDbContext>
{
    public LogisticsDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString =
            configuration.GetConnectionString("Postgres")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? "Host=localhost;Port=5432;Database=logistics;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<LogisticsDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new LogisticsDbContext(optionsBuilder.Options);
    }
}

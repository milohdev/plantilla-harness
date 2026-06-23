using Logistics.Application.Common.Interfaces;
using Logistics.Domain.Users;
using Logistics.Infrastructure.Identity;
using Logistics.Infrastructure.Idempotency;
using Logistics.Infrastructure.Persistence;
using Logistics.Infrastructure.Persistence.Interceptors;
using Logistics.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Logistics.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Usuario actual (placeholder hasta que exista auth real).
        services.AddSingleton<ICurrentUserProvider, PlaceholderCurrentUserProvider>();

        // Interceptor de auditoría.
        services.AddScoped<AuditableEntityInterceptor>();

        // EF Core + PostgreSQL.
        var postgresConnection = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres no está configurada.");

        services.AddDbContext<LogisticsDbContext>((sp, options) =>
        {
            options.UseNpgsql(postgresConnection);
            options.AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>());
        });

        // Redis (idempotencia).
        var redisConnection = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("ConnectionStrings:Redis no está configurada.");

        services.AddSingleton<IConnectionMultiplexer>(
            _ => ConnectionMultiplexer.Connect(redisConnection));
        services.AddScoped<IIdempotencyService, RedisIdempotencyService>();

        // Hashing nativo + seeder.
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        services.Configure<AdminUserOptions>(configuration.GetSection(AdminUserOptions.SectionName));
        services.AddScoped<DatabaseSeeder>();

        return services;
    }
}

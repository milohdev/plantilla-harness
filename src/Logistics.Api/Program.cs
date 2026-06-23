using Logistics.Api.Extensions;
using Logistics.Api.Middleware;
using Logistics.Application;
using Logistics.Infrastructure;
using Logistics.Infrastructure.Persistence;
using Logistics.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Serilog;

// Logger de arranque. Si Serilog falla, el host cae al ILogger nativo de .NET.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    var config = builder.Configuration;

    // Capas.
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(config);

    // API.
    builder.Services.AddControllers();
    builder.Services.AddApiRateLimiting();
    builder.Services.AddJwtAuthentication(config);
    builder.Services.AddSwaggerWithJwt();
    builder.Services.AddProblemDetails();

    // Health checks.
    builder.Services.AddHealthChecks()
        .AddNpgSql(
            config.GetConnectionString("Postgres")!,
            name: "postgres",
            tags: ["ready"])
        .AddRedis(
            config.GetConnectionString("Redis")!,
            name: "redis",
            tags: ["ready"]);

    var app = builder.Build();

    // ----- Pipeline (orden estricto) -----
    app.UseSerilogRequestLogging();
    app.UseMiddleware<CorrelationIdMiddleware>();   // R5
    app.UseRateLimiter();                            // R6
    app.UseExceptionHandler();                       // R7 (Problem Details nativo)
    app.UseStatusCodePages();
    app.UseAuthentication();                          // R8
    app.UseAuthorization();                           // R8
    app.UseMiddleware<IdempotencyMiddleware>();      // R9

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // Health checks.
    app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = _ => false // liveness: sin dependencias externas
    });
    app.MapHealthChecks("/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready")
    });

    app.MapControllers().RequireRateLimiting(ApiServiceCollectionExtensions.FixedRateLimitPolicy);

    // Migración + seed al arranque.
    await InitializeDatabaseAsync(app);

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "La aplicación terminó inesperadamente durante el arranque.");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

static async Task InitializeDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var sp = scope.ServiceProvider;

    var context = sp.GetRequiredService<LogisticsDbContext>();
    await context.Database.MigrateAsync();

    var seeder = sp.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedAsync();
}

public partial class Program;

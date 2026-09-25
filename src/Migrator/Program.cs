using Application.Common.Interfaces;
using Application.Common.Interfaces;
using Infrastructure;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;

// Standalone database migrator.
//
// This console app is the single place that applies EF Core migrations and seeds reference data.
// It runs as a one-shot container (Docker Compose, Container Apps job) before the API replicas
// start, so the API itself no longer has to migrate on startup and can safely scale out.
//
// Configuration is read from the same sources as the API (environment variables using the "__"
// separator, command line arguments). Example:
//
//   ConnectionStrings__DefaultConnection="Server=sqlserver,1433;Database=TicketBookingPlatform;..."
//   Seed__Admin__Email="admin@example.com"
//   Seed__Admin__Password="<strong-password>"
//   Migrator__SeedDemoData="true"
//
// The administrator account is always seeded (idempotently), because it is the only way an Admin
// user ever comes into existence. Only the demo fixtures (locations, events, ticket categories)
// are behind the "Migrator:SeedDemoData" switch.

const int ExitSuccess = 0;
const int ExitFailure = 1;
const int ExitInvalidConfiguration = 2;

ILogger logger = LoggerFactory
    .Create(loggingBuilder => loggingBuilder.AddConsole())
    .CreateLogger("Migrator");

try
{
    HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

    ConfigurationManager configuration = builder.Configuration;

    bool seedDemoData = configuration.GetValue("Migrator:SeedDemoData", false);

    if (configuration.GetConnectionString("DefaultConnection") is null)
    {
        logger.LogError(
            "Connection string 'ConnectionStrings:DefaultConnection' is not configured.");
        return ExitInvalidConfiguration;
    }

    if (configuration["Seed:Admin:Email"] is null)
    {
        logger.LogError(
            "'Seed:Admin:Email' is not configured; the administrator account cannot be seeded.");
        return ExitInvalidConfiguration;
    }

    builder.Services.AddPersistence(configuration);

    using IHost host = builder.Build();

    logger = host.Services.GetRequiredService<ILogger<Program>>();

    using IServiceScope scope = host.Services.CreateScope();
    IServiceProvider services = scope.ServiceProvider;

    ApplicationDbContext dbContext = services.GetRequiredService<ApplicationDbContext>();

    logger.LogInformation("Applying EF Core migrations...");
    await dbContext.Database.MigrateAsync();

    logger.LogInformation(seedDemoData ?
        "Seeding administrator account and demo data..." :
        "Seeding administrator account...");

    IPasswordHasher passwordHasher = services.GetRequiredService<IPasswordHasher>();
    await DbInitializer.SeedDataAsync(dbContext, passwordHasher, configuration, seedDemoData);

    logger.LogInformation("Database is up to date.");
    return ExitSuccess;
}
catch (Exception ex)
{
    logger.LogError(ex, "Database migration failed.");
    return ExitFailure;
}

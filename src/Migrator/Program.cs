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
//   Database__SeedOnStartup="true"

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.AddPersistence(builder.Configuration);

using IHost host = builder.Build();

using IServiceScope scope = host.Services.CreateScope();
IServiceProvider services = scope.ServiceProvider;

ILogger<Program> logger = services.GetRequiredService<ILogger<Program>>();
IConfiguration configuration = services.GetRequiredService<IConfiguration>();

try
{
    ApplicationDbContext dbContext = services.GetRequiredService<ApplicationDbContext>();

    logger.LogInformation("Applying EF Core migrations...");
    await dbContext.Database.MigrateAsync();

    bool seedOnStartup = configuration.GetValue("Database:SeedOnStartup", false);
    if (seedOnStartup)
    {
        logger.LogInformation("Seeding reference data...");
        IPasswordHasher passwordHasher = services.GetRequiredService<IPasswordHasher>();
        await DbInitializer.SeedDataAsync(dbContext, passwordHasher, configuration);
    }

    logger.LogInformation("Database is up to date.");
    return 0;
}
catch (Exception ex)
{
    logger.LogError(ex, "Database migration failed.");
    return 1;
}

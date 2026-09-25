using Application.Common.Interfaces;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace WebApi.Infrastructure;

public static class DatabaseInitializationExtensions
{
    // NOTE: applies migrations and seeds reference data from the application process.
    // This is only safe with a single replica, so it is restricted to the Development environment.
    // In containers and any multi-replica environment the dedicated migrator (src/Migrator) must be used.
    public static async Task MigrateAndSeedDatabaseAsync(this WebApplication app)
    {
        if (app.Environment.IsTesting())
        {
            return;
        }

        bool migrateOnStartup = app.Configuration.GetValue("Database:MigrateOnStartup", false);
        bool seedOnStartup = app.Configuration.GetValue("Database:SeedOnStartup", false);

        if (!migrateOnStartup && !seedOnStartup)
        {
            return;
        }

        if (!app.Environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                "Database:MigrateOnStartup/SeedOnStartup are only supported in the Development environment, " +
                "because applying migrations from the application process is unsafe with multiple replicas. " +
                "Use the dedicated migrator (src/Migrator) or a Container Apps job before starting the API.");
        }

        using IServiceScope scope = app.Services.CreateScope();
        var serviceProvider = scope.ServiceProvider;

        try
        {
            var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();

            if (migrateOnStartup)
            {
                await dbContext.Database.MigrateAsync();
            }

            if (seedOnStartup)
            {
                var passwordHasher = serviceProvider.GetRequiredService<IPasswordHasher>();
                await DbInitializer.SeedDataAsync(dbContext, passwordHasher, app.Configuration);
            }
        }
        catch (Exception ex)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while initializing the database.");
            throw;
        }
    }
}

using Application.Common.Interfaces;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace WebApi.Infrastructure;

public static class DatabaseInitializationExtensions
{
    // NOTE: applies migrations and seeds reference data from the application process.
    // This is unsafe with more than one replica and is meant to be replaced by a dedicated migrator job or a distributed lock.
    public static async Task MigrateAndSeedDatabaseAsync(this WebApplication app)
    {
        bool migrateOnStartup = app.Configuration.GetValue("Database:MigrateOnStartup", false);
        bool seedOnStartup = app.Configuration.GetValue("Database:SeedOnStartup", false);

        if (app.Environment.IsTesting())
        {
            return;
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

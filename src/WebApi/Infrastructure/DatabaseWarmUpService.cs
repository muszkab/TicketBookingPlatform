using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace WebApi.Infrastructure;

// Azure SQL (serverless) auto-pauses and wakes only on a real connection, so this opens one right
// after startup - a scheduled scale-out (08:00 cron) then also warms the DB. Best-effort only.
public sealed class DatabaseWarmUpService : BackgroundService
{
    private const int MaxAttempts = 5;
    private static readonly TimeSpan AttemptDelay = TimeSpan.FromSeconds(10);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<DatabaseWarmUpService> _logger;

    public DatabaseWarmUpService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<DatabaseWarmUpService> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_configuration.GetValue("Database:WarmUpOnStartup", true))
        {
            _logger.LogDebug("Database warm-up is disabled (Database:WarmUpOnStartup=false).");
            return;
        }

        if (_environment.IsTesting())
        {
            return;
        }

        for (int attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                return;
            }

            try
            {
                using IServiceScope scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                await dbContext.Database.ExecuteSqlRawAsync("SELECT 1", stoppingToken);

                _logger.LogInformation("Database warm-up succeeded on attempt {Attempt}.", attempt);
                return;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(
                    ex,
                    "Database warm-up attempt {Attempt}/{MaxAttempts} failed; retrying in {Delay}.",
                    attempt,
                    MaxAttempts,
                    AttemptDelay);

                if (attempt == MaxAttempts)
                {
                    _logger.LogError(ex, "Database warm-up gave up after {MaxAttempts} attempts.", MaxAttempts);
                    return;
                }

                try
                {
                    await Task.Delay(AttemptDelay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }
    }
}

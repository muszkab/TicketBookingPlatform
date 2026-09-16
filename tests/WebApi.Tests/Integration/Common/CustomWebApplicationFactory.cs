using Domain.Users;
using Infrastructure.Persistence;
using Infrastructure.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WebApi.Tests.Integration.Common;

/// <summary>
/// Boots the real application pipeline against a private SQLite in-memory database.
/// Runs under the "Testing" environment so <c>Program</c> skips the startup migration
/// and seeding, letting each test class control its own data.
/// </summary>
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string SigningKey = "integration-test-signing-key-which-is-long-enough-0123456789";
    public const string Issuer = "ticket-booking-tests";
    public const string Audience = "ticket-booking-tests-client";

    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // UseSetting feeds the WebApplicationBuilder's configuration directly, so these
        // values are visible to AddInfrastructure during service registration.
        builder.UseSetting("ConnectionStrings:DefaultConnection", "DataSource=:memory:");
        builder.UseSetting("JwtSettings:Issuer", Issuer);
        builder.UseSetting("JwtSettings:Audience", Audience);
        builder.UseSetting("JwtSettings:SigningKey", SigningKey);
        builder.UseSetting("JwtSettings:AccessTokenExpirationMinutes", "60");

        builder.ConfigureTestServices(services =>
        {
            // Remove the SQL Server registration completely, including the provider's
            // internal services, otherwise EF refuses to resolve two providers at once.
            RemoveSqlServerRegistrations(services);

            _connection.Open();

            services.AddDbContext<ApplicationDbContext>(options => options
                .UseSqlite(_connection)
                .ReplaceService<IModelCustomizer, SqliteModelCustomizer>());
        });
    }

    private static void RemoveSqlServerRegistrations(IServiceCollection services)
    {
        // .NET 9's AddDbContext registers IDbContextOptionsConfiguration<T> in addition to
        // DbContextOptions; leaving it behind keeps UseSqlServer applied to the options and
        // EF then sees two providers.
        List<ServiceDescriptor> doomed = services
            .Where(d =>
                d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>)
                || d.ServiceType == typeof(DbContextOptions)
                || d.ServiceType == typeof(ApplicationDbContext)
                || (d.ServiceType.IsGenericType
                    && d.ServiceType.GetGenericTypeDefinition().Name.StartsWith("IDbContextOptionsConfiguration", StringComparison.Ordinal)))
            .ToList();

        foreach (ServiceDescriptor descriptor in doomed)
        {
            services.Remove(descriptor);
        }
    }

    /// <summary>
    /// Ensures the SQLite schema exists. Called once per factory by the test base.
    /// </summary>
    public void EnsureDatabaseCreated()
    {
        using IServiceScope scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Database.EnsureCreated();
    }

    /// <summary>
    /// Creates a scope for direct database access from tests.
    /// </summary>
    public IServiceScope CreateScope() => Services.CreateScope();

    public string CreateAccessToken(User user)
        => CreateToken(user, SigningKey);

    /// <summary>
    /// A structurally valid token signed with a different key, used to assert that
    /// signature validation is actually enforced by the pipeline.
    /// </summary>
    public string CreateTokenWithInvalidSignature(User user)
        => CreateToken(user, "a-completely-different-signing-key-9876543210-abcdefghij");

    private static string CreateToken(User user, string signingKey)
    {
        IOptions<JwtSettings> settings = Options.Create(new JwtSettings
        {
            Issuer = Issuer,
            Audience = Audience,
            SigningKey = signingKey,
            AccessTokenExpirationMinutes = 60,
        });

        return new JwtTokenGenerator(settings).GenerateToken(user).AccessToken;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _connection.Dispose();
        }
    }
}

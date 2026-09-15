using Application.Common.Interfaces;
using Infrastructure.Persistence;
using Infrastructure.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Infrastructure.Tests;

public class DependencyInjectionTests
{
    private const string ConnectionString = "Server=(localdb)\\mssqllocaldb;Database=TicketBookingTests;Trusted_Connection=True;";
    private const string SigningKey = "this-is-a-very-long-development-signing-key-1234567890";

    private static IConfiguration BuildConfiguration(
        string? connectionString = ConnectionString,
        string? signingKey = SigningKey,
        bool includeJwtSection = true)
    {
        var values = new Dictionary<string, string?>();

        if (connectionString is not null)
            values["ConnectionStrings:DefaultConnection"] = connectionString;

        if (includeJwtSection)
        {
            values["JwtSettings:Issuer"] = "ticket-booking";
            values["JwtSettings:Audience"] = "ticket-booking-client";
            values["JwtSettings:AccessTokenExpirationMinutes"] = "45";

            if (signingKey is not null)
                values["JwtSettings:SigningKey"] = signingKey;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private static ServiceProvider BuildProvider(IConfiguration? configuration = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(configuration ?? BuildConfiguration());
        return services.BuildServiceProvider();
    }

    [Fact]
    public void AddInfrastructure_Should_Return_SameServiceCollection()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        IServiceCollection result = services.AddInfrastructure(BuildConfiguration());

        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddInfrastructure_Should_Register_ApplicationDbContext()
    {
        using ServiceProvider provider = BuildProvider();
        using IServiceScope scope = provider.CreateScope();

        scope.ServiceProvider.GetService<ApplicationDbContext>().Should().NotBeNull();
    }

    [Fact]
    public void AddInfrastructure_Should_Register_IApplicationDbContext_AsSameInstanceAsDbContext()
    {
        using ServiceProvider provider = BuildProvider();
        using IServiceScope scope = provider.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var abstraction = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        abstraction.Should().BeSameAs(context);
    }

    [Fact]
    public void AddInfrastructure_Should_Register_PasswordHasher_AsSingleton()
    {
        using ServiceProvider provider = BuildProvider();

        var first = provider.GetRequiredService<IPasswordHasher>();
        var second = provider.GetRequiredService<IPasswordHasher>();

        first.Should().BeOfType<PasswordHasher>();
        second.Should().BeSameAs(first);
    }

    [Fact]
    public void AddInfrastructure_Should_Register_JwtTokenGenerator_AsSingleton()
    {
        using ServiceProvider provider = BuildProvider();

        var first = provider.GetRequiredService<IJwtTokenGenerator>();
        var second = provider.GetRequiredService<IJwtTokenGenerator>();

        first.Should().BeOfType<JwtTokenGenerator>();
        second.Should().BeSameAs(first);
    }

    [Fact]
    public void AddInfrastructure_Should_Bind_JwtSettings()
    {
        using ServiceProvider provider = BuildProvider();

        JwtSettings settings = provider.GetRequiredService<IOptions<JwtSettings>>().Value;

        settings.Issuer.Should().Be("ticket-booking");
        settings.Audience.Should().Be("ticket-booking-client");
        settings.SigningKey.Should().Be(SigningKey);
        settings.AccessTokenExpirationMinutes.Should().Be(45);
    }

    [Fact]
    public void AddInfrastructure_Should_Configure_JwtBearer_As_DefaultScheme()
    {
        using ServiceProvider provider = BuildProvider();

        AuthenticationOptions options = provider.GetRequiredService<IOptions<AuthenticationOptions>>().Value;

        options.DefaultScheme.Should().Be(JwtBearerDefaults.AuthenticationScheme);
    }

    [Fact]
    public void AddInfrastructure_Should_Configure_TokenValidationParameters()
    {
        using ServiceProvider provider = BuildProvider();

        JwtBearerOptions options = provider
            .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);

        options.TokenValidationParameters.ValidateIssuer.Should().BeTrue();
        options.TokenValidationParameters.ValidIssuer.Should().Be("ticket-booking");
        options.TokenValidationParameters.ValidateAudience.Should().BeTrue();
        options.TokenValidationParameters.ValidAudience.Should().Be("ticket-booking-client");
        options.TokenValidationParameters.ValidateLifetime.Should().BeTrue();
        options.TokenValidationParameters.ValidateIssuerSigningKey.Should().BeTrue();
        options.TokenValidationParameters.IssuerSigningKey.Should().NotBeNull();
        options.TokenValidationParameters.ClockSkew.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void AddInfrastructure_Should_Use_SqlServerProvider()
    {
        using ServiceProvider provider = BuildProvider();
        using IServiceScope scope = provider.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        context.Database.ProviderName.Should().Be("Microsoft.EntityFrameworkCore.SqlServer");
    }

    [Fact]
    public void AddInfrastructure_Should_Throw_When_ConnectionStringMissing()
    {
        var services = new ServiceCollection();

        FluentActions.Invoking(() => services.AddInfrastructure(BuildConfiguration(connectionString: null)))
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*DefaultConnection*");
    }

    [Fact]
    public void AddInfrastructure_Should_Throw_When_JwtSectionMissing()
    {
        var services = new ServiceCollection();

        FluentActions.Invoking(() => services.AddInfrastructure(BuildConfiguration(includeJwtSection: false)))
            .Should().Throw<InvalidOperationException>()
            .WithMessage($"*{JwtSettings.SectionName}*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddInfrastructure_Should_Throw_When_SigningKeyMissingOrBlank(string? signingKey)
    {
        var services = new ServiceCollection();

        FluentActions.Invoking(() => services.AddInfrastructure(BuildConfiguration(signingKey: signingKey)))
            .Should().Throw<InvalidOperationException>()
            .WithMessage($"*{JwtSettings.SectionName}*");
    }

    [Fact]
    public void AddInfrastructure_Should_Register_Authorization()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(BuildConfiguration());

        services.Any(d => d.ServiceType.FullName is not null
                          && d.ServiceType.FullName.Contains("IAuthorizationService", StringComparison.Ordinal))
            .Should().BeTrue();
    }
}

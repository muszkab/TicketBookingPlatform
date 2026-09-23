using Application;
using Application.Common.Interfaces;
using Asp.Versioning;
using Infrastructure;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Scalar.AspNetCore;
using System;
using System.Text.Json.Serialization;
using WebApi.Infrastructure;

string[] apiVersions = ["v1", "v2"];

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// API versioning
builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
// One OpenAPI document per API version.
bool openApiEnabled = builder.Configuration.GetValue("OpenApi:Enabled", false);

if (openApiEnabled)
{
    foreach (string version in apiVersions)
    {
        builder.Services.AddOpenApi(version, options =>
        {
            options.AddDocumentTransformer<ApiInfoDocumentTransformer>();
            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
            options.AddSchemaTransformer<StringEnumSchemaTransformer>();
        });
    }
}

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>(name: "database", tags: [HealthCheckTags.Ready])
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: [HealthCheckTags.Live]);

builder.Services.AddObservability(builder.Configuration);

bool forwardedHeadersEnabled = builder.Configuration.GetValue("ForwardedHeaders:Enabled", false);

if (forwardedHeadersEnabled)
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });
}

const string SpaCorsPolicy = "SpaCorsPolicy";
string[] allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

if (allowedOrigins.Length > 0)
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(SpaCorsPolicy, policy =>
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });
}

var app = builder.Build();

bool migrateOnStartup = app.Configuration.GetValue("Database:MigrateOnStartup", false);
bool seedOnStartup = app.Configuration.GetValue("Database:SeedOnStartup", false);

if (!app.Environment.IsTesting() && (migrateOnStartup || seedOnStartup))
{
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

// Configure the HTTP request pipeline.
if (openApiEnabled)
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        foreach (string version in apiVersions)
        {
            options.AddDocument(version, $"Version {version[1..]}.0");
        }

        options.WithTitle("Ticket Booking Platform API")
               .AddPreferredSecuritySchemes("Bearer");
    });
}

if (forwardedHeadersEnabled)
{
    app.UseForwardedHeaders();
}

app.UseExceptionHandler();

if (app.Configuration.GetValue("Https:HstsEnabled", false))
{
    app.UseHsts();
}

if (app.Configuration.GetValue("Https:RedirectEnabled", true))
{
    app.UseHttpsRedirection();
}

if (allowedOrigins.Length > 0)
{
    app.UseCors(SpaCorsPolicy);
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains(HealthCheckTags.Live)
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains(HealthCheckTags.Ready)
});

app.Run();

// Exposed so integration tests can bootstrap the application via WebApplicationFactory.
public partial class Program { }

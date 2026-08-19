using Application;
using Application.Common.Interfaces;
using Asp.Versioning;
using Infrastructure;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
foreach (string version in apiVersions)
{
    builder.Services.AddOpenApi(version, options =>
    {
        options.AddDocumentTransformer<ApiInfoDocumentTransformer>();
        options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
        options.AddSchemaTransformer<StringEnumSchemaTransformer>();
    });
}

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// Apply pending EF Core migrations at startup.
using (IServiceScope scope = app.Services.CreateScope())
{
    var serviceProvider = scope.ServiceProvider;

    try
    {
        var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var passwordHasher = serviceProvider.GetRequiredService<IPasswordHasher>();
        await dbContext.Database.MigrateAsync();
        await DbInitializer.SeedDataAsync(dbContext, passwordHasher, builder.Configuration);
    }
    catch (Exception ex)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while applying database migrations.");
        throw;
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
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

app.UseExceptionHandler();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

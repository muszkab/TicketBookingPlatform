using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Scalar.AspNetCore;

namespace WebApi.Infrastructure;

public static class ApiDocumentationExtensions
{
    private const string EnabledKey = "OpenApi:Enabled";

    private static readonly string[] ApiVersions = ["v1", "v2"];

    public static IServiceCollection AddApiDocumentation(this IServiceCollection services, IConfiguration configuration)
    {
        if (!IsEnabled(configuration))
        {
            return services;
        }

        foreach (string version in ApiVersions)
        {
            services.AddOpenApi(version, options =>
            {
                options.AddDocumentTransformer<ApiInfoDocumentTransformer>();
                options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
                options.AddSchemaTransformer<StringEnumSchemaTransformer>();
            });
        }

        return services;
    }

    // Serves /openapi/{version}.json and the Scalar UI at /scalar (see README).
    // The document path is a contract: CI diffs it against the committed spec in
    // frontend/ticket-booking-web/openapi/v1.json, from which the Angular client is generated.
    public static WebApplication MapApiDocumentation(this WebApplication app)
    {
        if (!IsEnabled(app.Configuration))
        {
            return app;
        }

        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            foreach (string version in ApiVersions)
            {
                options.AddDocument(version, $"Version {version[1..]}.0");
            }

            options.WithTitle("Ticket Booking Platform API")
                   .AddPreferredSecuritySchemes("Bearer");
        });

        return app;
    }

    private static bool IsEnabled(IConfiguration configuration) =>
        configuration.GetValue(EnabledKey, false);
}

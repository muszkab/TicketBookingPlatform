using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebApi.Infrastructure;

public static class JsonSerializationExtensions
{
    /// <summary>
    /// Applies the same JSON settings to the MVC options (controllers) and to the HTTP options,
    /// which the OpenAPI document generator reads its schemas from.
    /// </summary>
    public static IServiceCollection AddApiJsonSerialization(this IServiceCollection services)
    {
        services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(options =>
            ApplySharedSettings(options.JsonSerializerOptions));

        services.ConfigureHttpJsonOptions(options =>
        {
            ApplySharedSettings(options.SerializerOptions);
            // Schema only: the default AllowReadingFromString would describe numbers as ["integer","string"].
            options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict;
        });

        return services;
    }

    private static void ApplySharedSettings(JsonSerializerOptions options)
    {
        options.Converters.Add(new JsonStringEnumConverter());
    }
}

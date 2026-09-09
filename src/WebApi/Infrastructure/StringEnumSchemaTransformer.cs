using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WebApi.Infrastructure;

/// <summary>
/// Represents enum types as string with the list of allowed names in OpenAPI,
/// matching the runtime behavior of <see cref="System.Text.Json.Serialization.JsonStringEnumConverter"/>.
/// </summary>
public sealed class StringEnumSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        var type = context.JsonTypeInfo.Type;
        if (!type.IsEnum)
        {
            return Task.CompletedTask;
        }

        schema.Type = "string";
        schema.Format = null;
        schema.Default = null;
        schema.Enum = Enum.GetNames(type)
            .Select(name => (IOpenApiAny)new OpenApiString(name))
            .ToList();

        // Enums that only appear as query parameters are inlined by default. The schema id
        // annotation makes the built-in reference transformer emit them as a reusable
        // components/schemas entry, so client generators create named enum types.
        schema.Annotations ??= new Dictionary<string, object>();
        schema.Annotations["x-schema-id"] = type.Name;

        return Task.CompletedTask;
    }
}

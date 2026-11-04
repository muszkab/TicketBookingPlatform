using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using System;
using System.Linq;
using System.Text.Json.Nodes;
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

        schema.Type = JsonSchemaType.String;
        schema.Format = null;
        schema.Enum = Enum.GetNames(type)
            .Select(name => (JsonNode)JsonValue.Create(name))
            .ToList();

        return Task.CompletedTask;
    }
}

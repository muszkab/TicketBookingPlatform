using Domain.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using WebApi.Tests.Integration.Common;

namespace WebApi.Tests.Integration;

/// <summary>
/// Guards the generated OpenAPI document against drift between the MVC JSON settings (runtime)
/// and the HTTP JSON settings the document generator uses (see AddApiJsonSerialization).
/// </summary>
public class OpenApiContractTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Enum_Schemas_Should_List_Names_Like_The_Runtime_Serializer()
    {
        JsonElement schemas = await GetSchemasAsync();

        string[] values = schemas.GetProperty(nameof(EventStatus)).GetProperty("enum")
            .EnumerateArray()
            .Select(v => v.GetString()!)
            .ToArray();

        values.Should().Equal(Enum.GetNames<EventStatus>());
    }

    [Fact]
    public async Task Numeric_Properties_Should_Not_Allow_Strings()
    {
        JsonElement schemas = await GetSchemasAsync();

        List<string> offenders = [];
        foreach (JsonProperty schema in schemas.EnumerateObject())
        {
            if (!schema.Value.TryGetProperty("properties", out JsonElement properties))
            {
                continue;
            }

            foreach (JsonProperty property in properties.EnumerateObject())
            {
                if (property.Value.TryGetProperty("type", out JsonElement type)
                    && type.ValueKind == JsonValueKind.Array
                    && type.EnumerateArray().Any(t => t.GetString() == "string")
                    && type.EnumerateArray().Any(t => t.GetString() is "integer" or "number"))
                {
                    offenders.Add($"{schema.Name}.{property.Name}");
                }
            }
        }

        offenders.Should().BeEmpty("numbers are serialized as JSON numbers, so the schema must not allow strings");
    }

    private async Task<JsonElement> GetSchemasAsync()
    {
        HttpClient client = CreateClient();

        HttpResponseMessage response = await client.GetAsync("/openapi/v1.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("components").GetProperty("schemas").Clone();
    }
}

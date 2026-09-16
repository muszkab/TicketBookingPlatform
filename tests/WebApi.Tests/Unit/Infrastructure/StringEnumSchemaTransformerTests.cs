using Domain.Events;
using Domain.Orders;
using Domain.Users;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using WebApi.Infrastructure;

namespace WebApi.Tests.Unit.Infrastructure;

public class StringEnumSchemaTransformerTests
{
    private readonly StringEnumSchemaTransformer _sut = new();

    private static OpenApiSchemaTransformerContext CreateContext(Type type)
        => new()
        {
            DocumentName = "v1",
            JsonTypeInfo = JsonSerializerOptions.Default.GetTypeInfo(type),
            JsonPropertyInfo = null,
            ParameterDescription = null,
            ApplicationServices = new ServiceCollection().BuildServiceProvider(),
        };

    private async Task<OpenApiSchema> TransformAsync(Type type, OpenApiSchema? schema = null)
    {
        schema ??= new OpenApiSchema();
        await _sut.TransformAsync(schema, CreateContext(type), CancellationToken.None);
        return schema;
    }

    private static string[] EnumValues(OpenApiSchema schema)
        => schema.Enum.Cast<OpenApiString>().Select(v => v.Value).ToArray();

    [Fact]
    public async Task TransformAsync_Should_Set_StringType_For_Enums()
    {
        OpenApiSchema schema = await TransformAsync(typeof(EventStatus));

        schema.Type.Should().Be("string");
    }

    [Fact]
    public async Task TransformAsync_Should_Clear_Format_For_Enums()
    {
        var schema = new OpenApiSchema { Type = "integer", Format = "int32" };

        await TransformAsync(typeof(EventStatus), schema);

        schema.Format.Should().BeNull("the integer format must not survive the string conversion");
    }

    [Fact]
    public async Task TransformAsync_Should_List_AllEnumNames()
    {
        OpenApiSchema schema = await TransformAsync(typeof(EventStatus));

        EnumValues(schema).Should().BeEquivalentTo(Enum.GetNames<EventStatus>());
    }

    [Theory]
    [InlineData(typeof(EventStatus))]
    [InlineData(typeof(EventCategory))]
    [InlineData(typeof(OrderStatus))]
    [InlineData(typeof(UserRole))]
    public async Task TransformAsync_Should_Handle_AllDomainEnums(Type enumType)
    {
        OpenApiSchema schema = await TransformAsync(enumType);

        schema.Type.Should().Be("string");
        EnumValues(schema).Should().BeEquivalentTo(Enum.GetNames(enumType));
    }

    [Fact]
    public async Task TransformAsync_Should_Emit_NamesNotNumericValues()
    {
        OpenApiSchema schema = await TransformAsync(typeof(UserRole));

        string[] values = EnumValues(schema);
        values.Should().Contain(nameof(UserRole.Admin));
        values.Should().OnlyContain(v => char.IsLetter(v[0]));
    }

    [Theory]
    [InlineData(typeof(string))]
    [InlineData(typeof(int))]
    [InlineData(typeof(Guid))]
    public async Task TransformAsync_Should_Ignore_NonEnumTypes(Type type)
    {
        var schema = new OpenApiSchema { Type = "integer", Format = "int32" };

        await TransformAsync(type, schema);

        schema.Type.Should().Be("integer", "non-enum schemas must be left untouched");
        schema.Format.Should().Be("int32");
        schema.Enum.Should().BeEmpty();
    }

    [Fact]
    public async Task TransformAsync_Should_Replace_PreviouslyGeneratedEnumValues()
    {
        var schema = new OpenApiSchema
        {
            Enum = { new OpenApiString("0"), new OpenApiString("1") },
        };

        await TransformAsync(typeof(EventStatus), schema);

        EnumValues(schema).Should().BeEquivalentTo(Enum.GetNames<EventStatus>());
    }
}

using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WebApi.Infrastructure;

namespace WebApi.Tests.Unit.Infrastructure;

public class BearerSecuritySchemeTransformerTests
{
    private readonly BearerSecuritySchemeTransformer _sut = new();

    private static OpenApiDocumentTransformerContext CreateContext()
        => new()
        {
            DocumentName = "v1",
            DescriptionGroups = Array.Empty<ApiDescriptionGroup>(),
            ApplicationServices = new ServiceCollection().BuildServiceProvider(),
        };

    private async Task<OpenApiDocument> TransformAsync(OpenApiDocument? document = null)
    {
        document ??= new OpenApiDocument();
        await _sut.TransformAsync(document, CreateContext(), CancellationToken.None);
        return document;
    }

    [Fact]
    public async Task TransformAsync_Should_Create_Components_When_Missing()
    {
        var document = new OpenApiDocument { Components = null };

        await TransformAsync(document);

        document.Components.Should().NotBeNull();
    }

    [Fact]
    public async Task TransformAsync_Should_Register_BearerScheme()
    {
        OpenApiDocument document = await TransformAsync();

        document.Components!.SecuritySchemes.Should().ContainKey("Bearer");
    }

    [Fact]
    public async Task TransformAsync_Should_Configure_BearerScheme_AsHttpJwt()
    {
        OpenApiDocument document = await TransformAsync();

        IOpenApiSecurityScheme scheme = document.Components!.SecuritySchemes!["Bearer"];
        scheme.Type.Should().Be(SecuritySchemeType.Http);
        scheme.Scheme.Should().Be("bearer");
        scheme.BearerFormat.Should().Be("JWT");
        scheme.In.Should().Be(ParameterLocation.Header);
        scheme.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task TransformAsync_Should_Add_GlobalSecurityRequirement()
    {
        OpenApiDocument document = await TransformAsync();

        document.Security.Should().ContainSingle();
    }

    [Fact]
    public async Task TransformAsync_Should_Reference_BearerScheme_InRequirement()
    {
        OpenApiDocument document = await TransformAsync();

        OpenApiSecurityRequirement requirement = document.Security!.Single();
        OpenApiSecuritySchemeReference key = requirement.Keys.Single();

        key.Reference.Type.Should().Be(ReferenceType.SecurityScheme);
        key.Reference.Id.Should().Be("Bearer");
        requirement[key].Should().BeEmpty("the bearer requirement carries no scopes");
    }

    [Fact]
    public async Task TransformAsync_Should_Preserve_ExistingComponents()
    {
        var document = new OpenApiDocument
        {
            Components = new OpenApiComponents
            {
                Schemas = new Dictionary<string, IOpenApiSchema>
                {
                    ["EventDto"] = new OpenApiSchema { Type = JsonSchemaType.Object },
                },
            },
        };

        await TransformAsync(document);

        document.Components.Schemas.Should().ContainKey("EventDto");
        document.Components.SecuritySchemes.Should().ContainKey("Bearer");
    }

    [Fact]
    public async Task TransformAsync_Should_Overwrite_ExistingBearerScheme()
    {
        var document = new OpenApiDocument
        {
            Components = new OpenApiComponents
            {
                SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
                {
                    ["Bearer"] = new OpenApiSecurityScheme { Type = SecuritySchemeType.ApiKey },
                },
            },
        };

        await TransformAsync(document);

        document.Components.SecuritySchemes["Bearer"].Type.Should().Be(SecuritySchemeType.Http);
    }
}

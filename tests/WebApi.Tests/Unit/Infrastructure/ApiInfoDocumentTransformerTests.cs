using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using System;
using System.Threading;
using System.Threading.Tasks;
using WebApi.Infrastructure;

namespace WebApi.Tests.Unit.Infrastructure;

public class ApiInfoDocumentTransformerTests
{
    private readonly ApiInfoDocumentTransformer _sut = new();

    private static OpenApiDocumentTransformerContext CreateContext(string documentName)
        => new()
        {
            DocumentName = documentName,
            DescriptionGroups = Array.Empty<ApiDescriptionGroup>(),
            ApplicationServices = new ServiceCollection().BuildServiceProvider(),
        };

    private async Task<OpenApiDocument> TransformAsync(string documentName = "v1", OpenApiDocument? document = null)
    {
        document ??= new OpenApiDocument();
        await _sut.TransformAsync(document, CreateContext(documentName), CancellationToken.None);
        return document;
    }

    [Fact]
    public async Task TransformAsync_Should_Create_Info_When_Missing()
    {
        var document = new OpenApiDocument { Info = null };

        await TransformAsync(document: document);

        document.Info.Should().NotBeNull();
    }

    [Theory]
    [InlineData("v1")]
    [InlineData("v2")]
    public async Task TransformAsync_Should_Set_Version_From_DocumentName(string documentName)
    {
        OpenApiDocument document = await TransformAsync(documentName);

        document.Info.Version.Should().Be(documentName);
    }

    [Theory]
    [InlineData("v1", "Ticket Booking Platform API - v1")]
    [InlineData("v2", "Ticket Booking Platform API - v2")]
    public async Task TransformAsync_Should_Set_VersionedTitle(string documentName, string expectedTitle)
    {
        OpenApiDocument document = await TransformAsync(documentName);

        document.Info.Title.Should().Be(expectedTitle);
    }

    [Fact]
    public async Task TransformAsync_Should_Set_Description()
    {
        OpenApiDocument document = await TransformAsync();

        document.Info.Description.Should().Be("Public REST API for the Ticket Booking Platform.");
    }

    [Fact]
    public async Task TransformAsync_Should_Set_Contact()
    {
        OpenApiDocument document = await TransformAsync();

        document.Info.Contact.Should().NotBeNull();
        document.Info.Contact!.Email.Should().Be("m1musbal@gmail.com");
        document.Info.Contact.Name.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task TransformAsync_Should_Set_MitLicense()
    {
        OpenApiDocument document = await TransformAsync();

        document.Info.License.Should().NotBeNull();
        document.Info.License!.Name.Should().Be("MIT");
        document.Info.License.Url.Should().Be(new Uri("https://opensource.org/licenses/MIT"));
    }

    [Fact]
    public async Task TransformAsync_Should_Overwrite_ExistingInfoValues()
    {
        var document = new OpenApiDocument
        {
            Info = new OpenApiInfo { Title = "stale title", Version = "stale" },
        };

        await TransformAsync("v2", document);

        document.Info.Title.Should().Be("Ticket Booking Platform API - v2");
        document.Info.Version.Should().Be("v2");
    }

    [Fact]
    public async Task TransformAsync_Should_Not_Modify_Paths()
    {
        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths { ["/api/v1/Events"] = new OpenApiPathItem() },
        };

        await TransformAsync(document: document);

        document.Paths.Should().ContainKey("/api/v1/Events");
    }
}

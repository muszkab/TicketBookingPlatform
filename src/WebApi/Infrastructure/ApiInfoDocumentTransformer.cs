using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace WebApi.Infrastructure;

public sealed class ApiInfoDocumentTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Info ??= new OpenApiInfo();
        document.Info.Title = "Ticket Booking Platform API";
        document.Info.Version = context.DocumentName;
        document.Info.Description = "Public REST API for the Ticket Booking Platform.";
        document.Info.Contact = new OpenApiContact
        {
            Name = "Balázs Muszka",
            Email = "m1musbal@gmail.com",
        };
        document.Info.License = new OpenApiLicense
        {
            Name = "MIT",
            Url = new Uri("https://opensource.org/licenses/MIT"),
        };

        return Task.CompletedTask;
    }
}

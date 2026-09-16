using Domain.Events;
using Domain.Locations;
using Domain.Users;
using Infrastructure.Persistence;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WebApi.Tests.Integration.Common;

namespace WebApi.Tests.Integration;

public class ErrorHandlingTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task UnknownResource_Should_Return_404()
    {
        HttpClient client = CreateClient();

        HttpResponseMessage response = await client.GetAsync($"/api/v1/Events/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task NotFoundException_Should_Be_Translated_To_ProblemDetails()
    {
        HttpClient client = CreateAuthenticatedClient(TestDataSeeder.NewUser(role: UserRole.Admin));

        // Publishing a non-existent event makes the handler throw NotFoundException.
        HttpResponseMessage response = await client.PostAsync(
            $"/api/v1/Events/{Guid.NewGuid()}/publish", EmptyJson());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

        JsonElement problem = await ReadProblemAsync(response);
        problem.GetProperty("status").GetInt32().Should().Be(404);
        problem.GetProperty("title").GetString().Should().Be("Resource not found");
    }

    [Fact]
    public async Task BusinessRuleViolation_Should_Be_Translated_To_409()
    {
        Guid eventId = await SeedOnSaleEventAsync();
        HttpClient client = CreateAuthenticatedClient(TestDataSeeder.NewUser(role: UserRole.Admin));

        // The event is already on sale, so publishing again violates a business rule.
        HttpResponseMessage response = await client.PostAsync(
            $"/api/v1/Events/{eventId}/publish", EmptyJson());

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        JsonElement problem = await ReadProblemAsync(response);
        problem.GetProperty("status").GetInt32().Should().Be(409);
        problem.GetProperty("title").GetString().Should().Be("Business rule violation");
    }

    [Fact]
    public async Task MalformedJson_Should_Return_400()
    {
        HttpClient client = CreateAuthenticatedClient(TestDataSeeder.NewUser(role: UserRole.Admin));
        var content = new StringContent("{ not json", Encoding.UTF8, "application/json");

        HttpResponseMessage response = await client.PostAsync("/api/v1/Events", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task InvalidRouteParameter_Should_Return_404()
    {
        HttpClient client = CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/v1/Events/not-a-guid");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "the :guid route constraint rejects the value before the action runs");
    }

    [Fact]
    public async Task ProblemDetails_Should_Not_Leak_StackTrace_Outside_Development()
    {
        HttpClient client = CreateAuthenticatedClient(TestDataSeeder.NewUser(role: UserRole.Admin));

        HttpResponseMessage response = await client.PostAsync(
            $"/api/v1/Events/{Guid.NewGuid()}/publish", EmptyJson());

        string body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain("   at ", "stack traces must never reach the client");
    }

    [Fact]
    public async Task ProblemDetails_Should_Include_TraceId()
    {
        HttpClient client = CreateAuthenticatedClient(TestDataSeeder.NewUser(role: UserRole.Admin));

        HttpResponseMessage response = await client.PostAsync(
            $"/api/v1/Events/{Guid.NewGuid()}/publish", EmptyJson());

        JsonElement problem = await ReadProblemAsync(response);
        problem.TryGetProperty("traceId", out JsonElement traceId).Should().BeTrue();
        traceId.GetString().Should().NotBeNullOrWhiteSpace();
    }

    private async Task<Guid> SeedOnSaleEventAsync()
    {
        return await ExecuteDbAsync(async (ApplicationDbContext db) =>
        {
            Location location = TestDataSeeder.NewLocation($"Venue-{Guid.NewGuid():N}");
            Event ev = TestDataSeeder.NewOnSaleEvent(location.Id, out _, $"Event-{Guid.NewGuid():N}");

            db.Locations.Add(location);
            db.Events.Add(ev);
            await db.SaveChangesAsync();

            return ev.Id;
        });
    }

    private static StringContent EmptyJson()
        => new("{}", Encoding.UTF8, "application/json");

    private static async Task<JsonElement> ReadProblemAsync(HttpResponseMessage response)
    {
        string json = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json).RootElement;
    }
}

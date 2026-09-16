using Domain.Events;
using Domain.Locations;
using Infrastructure.Persistence;
using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using WebApi.Tests.Integration.Common;

namespace WebApi.Tests.Integration;

public class SerializationContractTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Enums_Should_Be_Serialized_As_Strings()
    {
        await SeedEventAsync();
        HttpClient client = CreateClient();

        JsonElement payload = await GetJsonAsync(client, "/api/v1/Events");
        JsonElement first = payload.GetProperty("items").EnumerateArray().First();

        first.GetProperty("category").ValueKind.Should().Be(JsonValueKind.String);
        first.GetProperty("status").ValueKind.Should().Be(JsonValueKind.String);
        first.GetProperty("status").GetString().Should().Be(nameof(EventStatus.OnSale));
    }

    [Fact]
    public async Task Enum_QueryParameters_Should_Accept_StringValues()
    {
        await SeedEventAsync();
        HttpClient client = CreateClient();

        JsonElement payload = await GetJsonAsync(client, "/api/v1/Events?status=OnSale");

        payload.GetProperty("items").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task PagedResult_Should_Expose_ExpectedShape()
    {
        await SeedEventAsync();
        HttpClient client = CreateClient();

        JsonElement payload = await GetJsonAsync(client, "/api/v1/Events");

        payload.TryGetProperty("items", out _).Should().BeTrue();
        payload.TryGetProperty("page", out _).Should().BeTrue();
        payload.TryGetProperty("pageSize", out _).Should().BeTrue();
        payload.TryGetProperty("totalCount", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Properties_Should_Use_CamelCase()
    {
        await SeedEventAsync();
        HttpClient client = CreateClient();

        JsonElement payload = await GetJsonAsync(client, "/api/v1/Events");
        JsonElement first = payload.GetProperty("items").EnumerateArray().First();

        first.TryGetProperty("startsAt", out _).Should().BeTrue();
        first.TryGetProperty("locationId", out _).Should().BeTrue();
        first.TryGetProperty("StartsAt", out _).Should().BeFalse();
    }

    [Fact]
    public async Task TicketCategories_Should_Flatten_MoneyValueObject()
    {
        await SeedEventAsync();
        HttpClient client = CreateClient();

        JsonElement payload = await GetJsonAsync(client, "/api/v1/Events");
        JsonElement category = payload.GetProperty("items").EnumerateArray()
            .First()
            .GetProperty("ticketCategories").EnumerateArray().First();

        category.GetProperty("price").ValueKind.Should().Be(JsonValueKind.Number);
        category.GetProperty("currency").GetString().Should().Be("EUR");
    }

    [Fact]
    public async Task Paging_QueryParameters_Should_Be_Honoured()
    {
        await SeedEventAsync();
        await SeedEventAsync();
        HttpClient client = CreateClient();

        JsonElement payload = await GetJsonAsync(client, "/api/v1/Events?page=1&pageSize=1");

        payload.GetProperty("pageSize").GetInt32().Should().Be(1);
        payload.GetProperty("items").GetArrayLength().Should().Be(1);
    }

    private async Task SeedEventAsync()
    {
        await ExecuteDbAsync(async (ApplicationDbContext db) =>
        {
            Location location = TestDataSeeder.NewLocation($"Venue-{Guid.NewGuid():N}");
            Event ev = TestDataSeeder.NewOnSaleEvent(location.Id, out _, $"Event-{Guid.NewGuid():N}");

            db.Locations.Add(location);
            db.Events.Add(ev);
            await db.SaveChangesAsync();
        });
    }

    private static async Task<JsonElement> GetJsonAsync(HttpClient client, string url)
    {
        HttpResponseMessage response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        string json = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json).RootElement;
    }
}

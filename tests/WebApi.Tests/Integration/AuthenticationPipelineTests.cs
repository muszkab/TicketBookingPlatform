using Domain.Users;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using WebApi.Tests.Integration.Common;

namespace WebApi.Tests.Integration;

public class AuthenticationPipelineTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    private const string ProtectedEndpoint = "/api/v1/Orders/mine";
    private const string EventsEndpoint = "/api/v1/Events";

    [Fact]
    public async Task AnonymousRequest_To_ProtectedEndpoint_Should_Return_401()
    {
        HttpClient client = CreateClient();

        HttpResponseMessage response = await client.GetAsync(ProtectedEndpoint);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AnonymousRequest_To_AllowAnonymousEndpoint_Should_Succeed()
    {
        HttpClient client = CreateClient();

        HttpResponseMessage response = await client.GetAsync(EventsEndpoint);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ValidToken_Should_Authenticate_Successfully()
    {
        HttpClient client = CreateAuthenticatedClient(TestDataSeeder.NewUser());

        HttpResponseMessage response = await client.GetAsync(ProtectedEndpoint);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task TokenWithInvalidSignature_Should_Return_401()
    {
        string token = Factory.CreateTokenWithInvalidSignature(TestDataSeeder.NewUser());
        HttpClient client = CreateClientWithRawToken(token);

        HttpResponseMessage response = await client.GetAsync(ProtectedEndpoint);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "signature validation must be enforced by the pipeline");
    }

    [Fact]
    public async Task MalformedToken_Should_Return_401()
    {
        HttpClient client = CreateClientWithRawToken("not-a-jwt");

        HttpResponseMessage response = await client.GetAsync(ProtectedEndpoint);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Unauthorized_Response_Should_Include_WwwAuthenticateHeader()
    {
        HttpClient client = CreateClient();

        HttpResponseMessage response = await client.GetAsync(ProtectedEndpoint);

        response.Headers.WwwAuthenticate.Should().NotBeEmpty();
        response.Headers.WwwAuthenticate.Should().Contain(h => h.Scheme == "Bearer");
    }

    [Fact]
    public async Task CustomerRole_Should_Be_Forbidden_From_AdminOnlyEndpoint()
    {
        HttpClient client = CreateAuthenticatedClient(TestDataSeeder.NewUser(role: UserRole.Customer));

        HttpResponseMessage response = await client.PostAsync(EventsEndpoint, JsonContent(new
        {
            title = "X",
            description = "Y",
            category = "Concert",
            startsAt = "2030-01-01T10:00:00+00:00",
            endsAt = "2030-01-01T12:00:00+00:00",
            locationId = System.Guid.NewGuid(),
        }));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "Customer is neither Organizer nor Admin");
    }

    [Theory]
    [InlineData(UserRole.Organizer)]
    [InlineData(UserRole.Admin)]
    public async Task PrivilegedRoles_Should_Pass_Authorization(UserRole role)
    {
        HttpClient client = CreateAuthenticatedClient(TestDataSeeder.NewUser(role: role));

        HttpResponseMessage response = await client.PostAsync(EventsEndpoint, JsonContent(new
        {
            title = "X",
            description = "Y",
            category = "Concert",
            startsAt = "2030-01-01T10:00:00+00:00",
            endsAt = "2030-01-01T12:00:00+00:00",
            locationId = System.Guid.NewGuid(),
        }));

        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AuthorizationHeader_WithWrongScheme_Should_Return_401()
    {
        HttpClient client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", "dXNlcjpwYXNz");

        HttpResponseMessage response = await client.GetAsync(ProtectedEndpoint);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static StringContent JsonContent(object payload)
        => new(System.Text.Json.JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");
}

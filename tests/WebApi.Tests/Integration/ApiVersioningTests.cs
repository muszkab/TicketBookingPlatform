using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using WebApi.Tests.Integration.Common;

namespace WebApi.Tests.Integration;

public class ApiVersioningTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    [Theory]
    [InlineData("/api/v1/Events")]
    [InlineData("/api/v2/Events")]
    public async Task BothVersions_Of_EventsEndpoint_Should_Be_Routable(string url)
    {
        HttpClient client = CreateClient();

        HttpResponseMessage response = await client.GetAsync(url);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Response_Should_Report_SupportedApiVersions()
    {
        HttpClient client = CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/v1/Events");

        response.Headers.TryGetValues("api-supported-versions", out var values).Should().BeTrue();
        string header = string.Join(",", values!);
        header.Should().Contain("1.0");
    }

    [Fact]
    public async Task V2_Only_Controller_Should_Not_Be_Reachable_Through_V1_Segment()
    {
        HttpClient client = CreateClient();

        HttpResponseMessage v2 = await client.GetAsync("/api/v2/Events");
        v2.Headers.TryGetValues("api-supported-versions", out var values).Should().BeTrue();

        string.Join(",", values!).Should().Contain("2.0");
    }

    [Fact]
    public async Task UnknownVersion_Should_Not_Return_Success()
    {
        HttpClient client = CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/v9/Events");

        response.IsSuccessStatusCode.Should().BeFalse();
    }

    [Fact]
    public async Task VersionedRoute_Is_Required_For_Controllers()
    {
        HttpClient client = CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/Events");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "the route template requires an explicit api version segment");
    }

    [Theory]
    [InlineData("/api/v1/Locations")]
    [InlineData("/api/v1/Events")]
    public async Task PublicEndpoints_Should_Be_Versioned_And_Anonymous(string url)
    {
        HttpClient client = CreateClient();

        HttpResponseMessage response = await client.GetAsync(url);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

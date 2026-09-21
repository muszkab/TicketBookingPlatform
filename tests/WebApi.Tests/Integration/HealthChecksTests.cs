using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using WebApi.Tests.Integration.Common;

namespace WebApi.Tests.Integration;

public class HealthChecksTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Live_Endpoint_Should_Return_Healthy_Without_Requiring_Authentication()
    {
        HttpClient client = CreateClient();

        HttpResponseMessage response = await client.GetAsync("/health/live");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Ready_Endpoint_Should_Return_Healthy_When_Database_Is_Reachable()
    {
        HttpClient client = CreateClient();

        HttpResponseMessage response = await client.GetAsync("/health/ready");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Health_Endpoint_Should_Return_NotFound()
    {
        HttpClient client = CreateClient();

        HttpResponseMessage response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

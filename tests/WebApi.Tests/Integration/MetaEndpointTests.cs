using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using WebApi.Contracts.Meta;
using WebApi.Infrastructure;
using WebApi.Tests.Integration.Common;

namespace WebApi.Tests.Integration;

public class MetaEndpointTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Meta_Endpoint_Should_Return_The_Version_Info_Without_Authentication()
    {
        HttpClient client = CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/v1/meta");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        VersionInfoResponse? info = await response.Content.ReadFromJsonAsync<VersionInfoResponse>(JsonOptions);

        info.Should().NotBeNull();
        info!.Version.Should().NotBeEmpty();
        info.Commit.Should().MatchRegex("^([0-9a-f]{7,40})?$");
        info.RuntimeVersion.Should().Be(Environment.Version.ToString());
        info.RuntimeMajor.Should().Be(Environment.Version.Major);
        info.BuildDate.Should().NotBeNull();
        info.Environment.Should().Be(AppEnvironments.Testing);
    }

    [Fact]
    public async Task Meta_Endpoint_Should_Not_Be_Cached()
    {
        HttpClient client = CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/v1/meta");

        response.Headers.CacheControl.Should().NotBeNull();
        response.Headers.CacheControl!.NoStore.Should().BeTrue();
    }
}

using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApi.Contracts.Meta;
using WebApi.Infrastructure;

namespace WebApi.Controllers.V1;

[ApiVersion("1.0")]
[AllowAnonymous]
public class MetaController(VersionInfoProvider versionInfoProvider) : ApiControllerBase
{
    [HttpGet(Name = nameof(GetVersionInfo))]
    [ProducesResponseType(typeof(VersionInfoResponse), StatusCodes.Status200OK)]
    public ActionResult<VersionInfoResponse> GetVersionInfo()
    {
        Response.Headers.CacheControl = "no-store";

        return Ok(new VersionInfoResponse(
            versionInfoProvider.Version,
            versionInfoProvider.Commit,
            versionInfoProvider.RuntimeVersion,
            versionInfoProvider.RuntimeMajor,
            versionInfoProvider.BuildDate,
            versionInfoProvider.EnvironmentName));
    }
}

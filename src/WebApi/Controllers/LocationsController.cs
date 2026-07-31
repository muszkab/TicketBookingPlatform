using Application.Locations.Queries.GetLocations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LocationsController : ControllerBase
{
    private readonly GetLocationsQueryHandler _getLocationsQueryHandler;

    public LocationsController(GetLocationsQueryHandler getLocationsQueryHandler)
    {
        _getLocationsQueryHandler = getLocationsQueryHandler;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<LocationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LocationDto>>> GetAll(CancellationToken cancellationToken)
    {
        var locations = await _getLocationsQueryHandler.HandleAsync(new GetLocationsQuery(), cancellationToken);
        return Ok(locations);
    }
}

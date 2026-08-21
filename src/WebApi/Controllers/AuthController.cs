using Application.Auth;
using Application.Auth.Commands.Login;
using Application.Auth.Commands.RegisterUser;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;
using WebApi.Contracts.Auth;

namespace WebApi.Controllers;

[AllowAnonymous]
public class AuthController(
    RegisterUserCommandHandler registerUserCommandHandler,
    LoginCommandHandler loginCommandHandler)
    : ApiControllerBase
{
    [HttpPost("register", Name = nameof(Register))]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterUserRequest request, CancellationToken cancellationToken)
    {
        await registerUserCommandHandler.HandleAsync(new RegisterUserCommand(request.Email, request.Password, request.FullName), cancellationToken);
        return StatusCode(StatusCodes.Status201Created); // TODO CreatedAtAction or AuthResultDto ?
    }

    [HttpPost("login", Name = nameof(Login))]
    [ProducesResponseType(typeof(AuthResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResultDto>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        AuthResultDto? result = await loginCommandHandler.HandleAsync(new LoginCommand(request.Email, request.Password), cancellationToken);
        return result is null ? Unauthorized() : Ok(result);
    }
}

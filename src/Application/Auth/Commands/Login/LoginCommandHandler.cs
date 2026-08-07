using Application.Common.Interfaces;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Auth.Commands.Login;

public sealed class LoginCommandHandler
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public LoginCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<AuthResultDto?> HandleAsync(LoginCommand command, CancellationToken cancellationToken = default)
    {
        string normalizedEmail = (command.Email ?? string.Empty).Trim().ToLowerInvariant();

        User? user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (user is null)
            return null;

        if (!_passwordHasher.Verify(user.PasswordHash, command.Password ?? string.Empty))
            return null;

        (string accessToken, DateTimeOffset expiresAt) = _jwtTokenGenerator.GenerateToken(user);

        return new AuthResultDto(
            user.Id,
            user.Email,
            user.FullName,
            user.Role,
            accessToken,
            expiresAt);
    }
}

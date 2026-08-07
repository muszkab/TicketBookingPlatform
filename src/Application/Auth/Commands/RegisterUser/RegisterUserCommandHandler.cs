using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Auth.Commands.RegisterUser;

public sealed class RegisterUserCommandHandler
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public RegisterUserCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<AuthResultDto> HandleAsync(RegisterUserCommand command, CancellationToken cancellationToken = default)
    {
        string normalizedEmail = (command.Email ?? string.Empty).Trim().ToLowerInvariant();

        bool emailTaken = await _context.Users
            .AsNoTracking()
            .AnyAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (emailTaken)
            throw new ConflictException($"A user with email '{normalizedEmail}' already exists.");

        string passwordHash = _passwordHasher.Hash(command.Password);

        var user = new User(normalizedEmail, passwordHash, command.FullName, UserRole.Customer);

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        (string accessToken, System.DateTimeOffset expiresAt) = _jwtTokenGenerator.GenerateToken(user);

        return new AuthResultDto(
            user.Id,
            user.Email,
            user.FullName,
            user.Role,
            accessToken,
            expiresAt);
    }
}

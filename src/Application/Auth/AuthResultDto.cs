using System;
using Domain.Users;

namespace Application.Auth;

public sealed record AuthResultDto(
    Guid UserId,
    string Email,
    string FullName,
    UserRole Role,
    string AccessToken,
    DateTimeOffset ExpiresAt);

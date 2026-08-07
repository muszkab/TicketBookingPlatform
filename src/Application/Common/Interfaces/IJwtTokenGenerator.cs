using Domain.Users;
using System;

namespace Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    (string AccessToken, DateTimeOffset ExpiresAt) GenerateToken(User user);
}

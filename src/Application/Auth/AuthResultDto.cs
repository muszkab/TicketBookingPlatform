using System;

namespace Application.Auth;

public sealed record AuthResultDto(
    string AccessToken,
    DateTimeOffset ExpiresAt);

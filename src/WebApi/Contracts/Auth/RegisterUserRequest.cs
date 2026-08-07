namespace WebApi.Contracts.Auth;

public sealed record RegisterUserRequest(
    string Email,
    string Password,
    string FullName);

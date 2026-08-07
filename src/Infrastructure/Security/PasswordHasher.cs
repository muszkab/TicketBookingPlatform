using Application.Common.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Security;

public sealed class PasswordHasher : IPasswordHasher
{
    private static readonly PasswordHasher<object> _inner = new();
    private static readonly object _placeholder = new();

    public string Hash(string password)
        => _inner.HashPassword(_placeholder, password);

    public bool Verify(string hash, string password)
    {
        PasswordVerificationResult result = _inner.VerifyHashedPassword(_placeholder, hash, password);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}

using System;
using Domain.Common;

namespace Domain.Users;

public class User : Entity
{
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public string FullName { get; private set; }
    public UserRole Role { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private User() : base(EmptyId)
    {
        Email = string.Empty;
        PasswordHash = string.Empty;
        FullName = string.Empty;
    }

    public User(
        string email,
        string passwordHash,
        string fullName,
        UserRole role = UserRole.Customer)
    {
        Validate(email, passwordHash, fullName);

        Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
        FullName = fullName;
        Role = role;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateProfile(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name is required.", nameof(fullName));

        FullName = fullName;
    }

    public void ChangeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));

        Email = email.Trim().ToLowerInvariant();
    }

    public void ChangePassword(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));

        PasswordHash = passwordHash;
    }

    public void ChangeRole(UserRole role) => Role = role;

    private static void Validate(string email, string passwordHash, string fullName)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));

        if (!email.Contains('@'))
            throw new ArgumentException("Email is invalid.", nameof(email));

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));

        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name is required.", nameof(fullName));
    }
}

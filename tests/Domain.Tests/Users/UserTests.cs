using Domain.Users;
using System;

namespace Domain.Tests.Users;

public class UserTests
{
    [Theory]
    [InlineData("", "hash", "Full Name")]
    [InlineData("no-at-sign", "hash", "Full Name")]
    [InlineData("a@b.com", "", "Full Name")]
    [InlineData("a@b.com", "hash", "")]
    public void Ctor_Should_Throw_When_InvalidInput(string email, string hash, string fullName)
    {
        Action act = () => new User(email, hash, fullName);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Ctor_Should_NormalizeEmail_And_DefaultToCustomer()
    {
        var user = new User("  Foo@Bar.COM ", "hash", "Foo Bar");
        user.Email.Should().Be("foo@bar.com");
        user.Role.Should().Be(UserRole.Customer);
        user.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void UpdateProfile_Should_Throw_When_FullNameEmpty()
    {
        var user = new User("a@b.com", "h", "Full");
        Action act = () => user.UpdateProfile(" ");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateProfile_Should_SetFullName()
    {
        var user = new User("a@b.com", "h", "Full");
        user.UpdateProfile("New");
        user.FullName.Should().Be("New");
    }

    [Fact]
    public void ChangeEmail_Should_LowerCaseAndTrim()
    {
        var user = new User("a@b.com", "h", "Full");
        user.ChangeEmail("  X@Y.COM ");
        user.Email.Should().Be("x@y.com");
    }

    [Fact]
    public void ChangeEmail_Should_Throw_When_Empty()
    {
        var user = new User("a@b.com", "h", "Full");
        Action act = () => user.ChangeEmail(" ");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ChangePassword_Should_Set_And_ValidateEmpty()
    {
        var user = new User("a@b.com", "h", "Full");
        user.ChangePassword("new-hash");
        user.PasswordHash.Should().Be("new-hash");

        Action act = () => user.ChangePassword(" ");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ChangeRole_Should_UpdateRole()
    {
        var user = new User("a@b.com", "h", "Full");
        user.ChangeRole(UserRole.Admin);
        user.Role.Should().Be(UserRole.Admin);
    }
}

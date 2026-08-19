using Application.Auth;
using Application.Auth.Commands.RegisterUser;
using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Tests.Common;
using Domain.Users;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Tests.Auth.Commands;

public class RegisterUserCommandHandlerTests
{
    private static (RegisterUserCommandHandler handler, IPasswordHasher hasher, IJwtTokenGenerator jwt, Infrastructure.Persistence.ApplicationDbContext db) BuildSut()
    {
        var db = TestDbContextFactory.CreateInMemory();
        var hasher = Substitute.For<IPasswordHasher>();
        var jwt = Substitute.For<IJwtTokenGenerator>();
        return (new RegisterUserCommandHandler(db, hasher, jwt), hasher, jwt, db);
    }

    [Fact]
    public async Task Handle_Should_Throw_Conflict_When_EmailAlreadyExists()
    {
        var (handler, _, _, db) = BuildSut();
        db.Users.Add(DomainFactory.NewUser("dup@example.com"));
        await db.SaveChangesAsync();

        var command = new RegisterUserCommand("  DUP@Example.com  ", "pw", "Full Name");

        await FluentActions.Invoking(() => handler.HandleAsync(command))
            .Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_Should_HashPassword_PersistUser_And_ReturnToken()
    {
        var (handler, hasher, jwt, db) = BuildSut();
        hasher.Hash("pw").Returns("HASHED");
        var expiresAt = DateTimeOffset.UtcNow.AddHours(1);
        jwt.GenerateToken(Arg.Any<User>()).Returns(("token", expiresAt));

        var result = await handler.HandleAsync(new RegisterUserCommand("New@User.com", "pw", "New User"));

        result.Should().Be(new AuthResultDto("token", expiresAt));

        hasher.Received(1).Hash("pw");
        jwt.Received(1).GenerateToken(Arg.Is<User>(u =>
            u.Email == "new@user.com" &&
            u.PasswordHash == "HASHED" &&
            u.FullName == "New User" &&
            u.Role == UserRole.Customer));

        db.Users.Single().Email.Should().Be("new@user.com");
    }
}

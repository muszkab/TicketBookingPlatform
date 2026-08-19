using Application.Auth;
using Application.Auth.Commands.Login;
using Application.Common.Interfaces;
using Application.Tests.Common;
using Domain.Users;
using System;
using System.Threading.Tasks;

namespace Application.Tests.Auth.Commands;

public class LoginCommandHandlerTests
{
    private static (LoginCommandHandler handler, IPasswordHasher hasher, IJwtTokenGenerator jwt, Infrastructure.Persistence.ApplicationDbContext db) BuildSut()
    {
        var db = TestDbContextFactory.CreateInMemory();
        var hasher = Substitute.For<IPasswordHasher>();
        var jwt = Substitute.For<IJwtTokenGenerator>();
        return (new LoginCommandHandler(db, hasher, jwt), hasher, jwt, db);
    }

    [Fact]
    public async Task Handle_Should_ReturnNull_When_UserNotFound()
    {
        var (handler, _, _, _) = BuildSut();
        var result = await handler.HandleAsync(new LoginCommand("missing@example.com", "pw"));
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Should_ReturnNull_When_PasswordInvalid()
    {
        var (handler, hasher, _, db) = BuildSut();
        db.Users.Add(DomainFactory.NewUser("u@x.com", passwordHash: "H"));
        await db.SaveChangesAsync();
        hasher.Verify("H", "wrong").Returns(false);

        var result = await handler.HandleAsync(new LoginCommand("u@x.com", "wrong"));

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Should_ReturnToken_When_CredentialsValid()
    {
        var (handler, hasher, jwt, db) = BuildSut();
        db.Users.Add(DomainFactory.NewUser("u@x.com", passwordHash: "H"));
        await db.SaveChangesAsync();
        hasher.Verify("H", "pw").Returns(true);
        var expiresAt = DateTimeOffset.UtcNow.AddHours(1);
        jwt.GenerateToken(Arg.Any<User>()).Returns(("tok", expiresAt));

        var result = await handler.HandleAsync(new LoginCommand("  U@X.com ", "pw"));

        result.Should().Be(new AuthResultDto("tok", expiresAt));
    }
}

using Domain.Users;
using Infrastructure.Security;
using Infrastructure.Tests.Common;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;

namespace Infrastructure.Tests.Security;

public class JwtTokenGeneratorTests
{
    private const string SigningKey = "this-is-a-very-long-development-signing-key-1234567890";

    private static JwtSettings Settings(int expirationMinutes = 60) => new()
    {
        Issuer = "ticket-booking-tests",
        Audience = "ticket-booking-client",
        SigningKey = SigningKey,
        AccessTokenExpirationMinutes = expirationMinutes,
    };

    private static JwtTokenGenerator CreateSut(JwtSettings? settings = null)
        => new(Options.Create(settings ?? Settings()));

    private static JwtSecurityToken Read(string accessToken)
        => new JwtSecurityTokenHandler().ReadJwtToken(accessToken);

    [Fact]
    public void GenerateToken_Should_Return_NonEmptyToken_And_FutureExpiry()
    {
        JwtTokenGenerator sut = CreateSut();
        User user = DomainFactory.NewUser();

        (string accessToken, DateTimeOffset expiresAt) = sut.GenerateToken(user);

        accessToken.Should().NotBeNullOrWhiteSpace();
        expiresAt.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public void GenerateToken_Should_Honor_ConfiguredExpiration()
    {
        JwtTokenGenerator sut = CreateSut(Settings(expirationMinutes: 15));
        DateTimeOffset before = DateTimeOffset.UtcNow;

        (_, DateTimeOffset expiresAt) = sut.GenerateToken(DomainFactory.NewUser());

        expiresAt.Should().BeCloseTo(before.AddMinutes(15), TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void GenerateToken_Should_Set_IssuerAndAudience()
    {
        JwtSettings settings = Settings();
        JwtTokenGenerator sut = CreateSut(settings);

        (string accessToken, _) = sut.GenerateToken(DomainFactory.NewUser());

        JwtSecurityToken token = Read(accessToken);
        token.Issuer.Should().Be(settings.Issuer);
        token.Audiences.Should().ContainSingle().Which.Should().Be(settings.Audience);
    }

    [Fact]
    public void GenerateToken_Should_Contain_ExpectedClaims()
    {
        JwtTokenGenerator sut = CreateSut();
        User user = DomainFactory.NewUser(email: "Admin@Example.com", fullName: "Admin User", role: UserRole.Admin);

        (string accessToken, _) = sut.GenerateToken(user);

        JwtSecurityToken token = Read(accessToken);
        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == user.Id.ToString());
        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == user.Email);
        token.Claims.Should().Contain(c => c.Value == user.FullName);
        token.Claims.Should().Contain(c => c.Value == nameof(UserRole.Admin));
        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Jti);
    }

    [Fact]
    public void GenerateToken_Should_Use_NormalizedEmail()
    {
        JwtTokenGenerator sut = CreateSut();
        User user = DomainFactory.NewUser(email: "  MiXeD@Example.COM  ");

        (string accessToken, _) = sut.GenerateToken(user);

        Read(accessToken).Claims
            .Single(c => c.Type == JwtRegisteredClaimNames.Email).Value
            .Should().Be("mixed@example.com");
    }

    [Fact]
    public void GenerateToken_Should_Emit_UniqueJti_PerToken()
    {
        JwtTokenGenerator sut = CreateSut();
        User user = DomainFactory.NewUser();

        string first = Read(sut.GenerateToken(user).AccessToken).Claims.Single(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
        string second = Read(sut.GenerateToken(user).AccessToken).Claims.Single(c => c.Type == JwtRegisteredClaimNames.Jti).Value;

        first.Should().NotBe(second);
    }

    [Fact]
    public void GenerateToken_Should_Use_HmacSha256()
    {
        JwtTokenGenerator sut = CreateSut();

        (string accessToken, _) = sut.GenerateToken(DomainFactory.NewUser());

        Read(accessToken).Header.Alg.Should().Be(SecurityAlgorithms.HmacSha256);
    }

    [Fact]
    public void GeneratedToken_Should_BeValidatable_WithSameSettings()
    {
        JwtSettings settings = Settings();
        JwtTokenGenerator sut = CreateSut(settings);
        User user = DomainFactory.NewUser(role: UserRole.Admin);

        (string accessToken, _) = sut.GenerateToken(user);

        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = settings.Issuer,
            ValidateAudience = true,
            ValidAudience = settings.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey)),
            ClockSkew = TimeSpan.FromSeconds(30),
        };

        ClaimsPrincipal principal = new JwtSecurityTokenHandler()
            .ValidateToken(accessToken, parameters, out SecurityToken _);

        principal.IsInRole(nameof(UserRole.Admin)).Should().BeTrue();
    }

    [Fact]
    public void GeneratedToken_Should_FailValidation_WithDifferentSigningKey()
    {
        JwtTokenGenerator sut = CreateSut();
        (string accessToken, _) = sut.GenerateToken(DomainFactory.NewUser());

        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes("a-completely-different-signing-key-0987654321")),
        };

        FluentActions.Invoking(() => new JwtSecurityTokenHandler()
                .ValidateToken(accessToken, parameters, out SecurityToken _))
            .Should().Throw<SecurityTokenSignatureKeyNotFoundException>();
    }

    [Fact]
    public void GeneratedToken_ValidTo_Should_Match_ReturnedExpiry()
    {
        JwtTokenGenerator sut = CreateSut(Settings(expirationMinutes: 5));

        (string accessToken, DateTimeOffset expiresAt) = sut.GenerateToken(DomainFactory.NewUser());

        Read(accessToken).ValidTo.Should().BeCloseTo(expiresAt.UtcDateTime, TimeSpan.FromSeconds(1));
    }
}

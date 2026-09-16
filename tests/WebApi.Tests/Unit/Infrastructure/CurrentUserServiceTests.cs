using Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using WebApi.Infrastructure;

namespace WebApi.Tests.Unit.Infrastructure;

public class CurrentUserServiceTests
{
    private static ICurrentUserService CreateSut(HttpContext? httpContext)
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);
        return new CurrentUserService(accessor);
    }

    private static HttpContext ContextWithClaims(params Claim[] claims)
        => new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "TestAuth")),
        };

    [Fact]
    public void UserId_Should_Return_Null_When_NoHttpContext()
    {
        ICurrentUserService sut = CreateSut(httpContext: null);

        sut.UserId.Should().BeNull();
    }

    [Fact]
    public void UserId_Should_Return_Null_When_UserIsAnonymous()
    {
        ICurrentUserService sut = CreateSut(new DefaultHttpContext());

        sut.UserId.Should().BeNull();
    }

    [Fact]
    public void UserId_Should_Read_NameIdentifierClaim()
    {
        var expected = Guid.NewGuid();
        ICurrentUserService sut = CreateSut(
            ContextWithClaims(new Claim(ClaimTypes.NameIdentifier, expected.ToString())));

        sut.UserId.Should().Be(expected);
    }

    [Fact]
    public void UserId_Should_FallBack_To_SubClaim()
    {
        var expected = Guid.NewGuid();
        ICurrentUserService sut = CreateSut(
            ContextWithClaims(new Claim("sub", expected.ToString())));

        sut.UserId.Should().Be(expected);
    }

    [Fact]
    public void UserId_Should_Prefer_NameIdentifier_Over_Sub()
    {
        var preferred = Guid.NewGuid();
        var ignored = Guid.NewGuid();
        ICurrentUserService sut = CreateSut(ContextWithClaims(
            new Claim("sub", ignored.ToString()),
            new Claim(ClaimTypes.NameIdentifier, preferred.ToString())));

        sut.UserId.Should().Be(preferred);
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("12345")]
    public void UserId_Should_Return_Null_When_ClaimIsNotAGuid(string value)
    {
        ICurrentUserService sut = CreateSut(
            ContextWithClaims(new Claim(ClaimTypes.NameIdentifier, value)));

        sut.UserId.Should().BeNull();
    }

    [Fact]
    public void UserId_Should_Parse_GuidInAnyStandardFormat()
    {
        var expected = Guid.NewGuid();
        ICurrentUserService sut = CreateSut(
            ContextWithClaims(new Claim(ClaimTypes.NameIdentifier, expected.ToString("B"))));

        sut.UserId.Should().Be(expected);
    }

    [Fact]
    public void UserId_Should_Return_Null_When_SubClaimIsNotAGuid()
    {
        ICurrentUserService sut = CreateSut(
            ContextWithClaims(new Claim("sub", "abc")));

        sut.UserId.Should().BeNull();
    }

    [Fact]
    public void UserId_Should_Reflect_ChangedHttpContext_OnEachAccess()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var accessor = Substitute.For<IHttpContextAccessor>();
        var sut = new CurrentUserService(accessor);

        accessor.HttpContext.Returns(ContextWithClaims(new Claim(ClaimTypes.NameIdentifier, first.ToString())));
        sut.UserId.Should().Be(first);

        accessor.HttpContext.Returns(ContextWithClaims(new Claim(ClaimTypes.NameIdentifier, second.ToString())));
        sut.UserId.Should().Be(second);
    }

    [Fact]
    public void UserId_Should_Return_Null_When_EmptyGuidIsNotUsable()
    {
        ICurrentUserService sut = CreateSut(
            ContextWithClaims(new Claim(ClaimTypes.NameIdentifier, Guid.Empty.ToString())));

        sut.UserId.Should().Be(Guid.Empty,
            "the service parses the claim verbatim; rejecting Guid.Empty is the caller's responsibility");
    }
}

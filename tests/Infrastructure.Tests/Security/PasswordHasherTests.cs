using Application.Common.Interfaces;
using Infrastructure.Security;

namespace Infrastructure.Tests.Security;

public class PasswordHasherTests
{
    private readonly IPasswordHasher _sut = new PasswordHasher();

    [Fact]
    public void Hash_Should_Not_Return_PlainTextPassword()
    {
        const string password = "Sup3rS3cret!";

        string hash = _sut.Hash(password);

        hash.Should().NotBeNullOrWhiteSpace();
        hash.Should().NotBe(password);
        hash.Should().NotContain(password);
    }

    [Fact]
    public void Hash_Should_Produce_DifferentHashes_ForSamePassword()
    {
        const string password = "Sup3rS3cret!";

        string first = _sut.Hash(password);
        string second = _sut.Hash(password);

        first.Should().NotBe(second, "a random salt must be used per hash");
    }

    [Fact]
    public void Verify_Should_ReturnTrue_ForMatchingPassword()
    {
        const string password = "Sup3rS3cret!";
        string hash = _sut.Hash(password);

        _sut.Verify(hash, password).Should().BeTrue();
    }

    [Theory]
    [InlineData("sup3rs3cret!")]
    [InlineData("Sup3rS3cret")]
    [InlineData("")]
    public void Verify_Should_ReturnFalse_ForNonMatchingPassword(string candidate)
    {
        string hash = _sut.Hash("Sup3rS3cret!");

        _sut.Verify(hash, candidate).Should().BeFalse();
    }

    [Fact]
    public void Verify_Should_ReturnFalse_ForHashOfAnotherPassword()
    {
        string hash = _sut.Hash("FirstPassword1");

        _sut.Verify(hash, "SecondPassword1").Should().BeFalse();
    }

    [Fact]
    public void Verify_Should_BeCaseSensitive()
    {
        string hash = _sut.Hash("Password1");

        _sut.Verify(hash, "password1").Should().BeFalse();
        _sut.Verify(hash, "Password1").Should().BeTrue();
    }
}

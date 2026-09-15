using Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace Infrastructure.Tests.Security;

public class JwtSettingsTests
{
    [Fact]
    public void SectionName_Should_Be_JwtSettings()
    {
        JwtSettings.SectionName.Should().Be("JwtSettings");
    }

    [Fact]
    public void Defaults_Should_Be_EmptyStrings_And_SixtyMinutes()
    {
        var settings = new JwtSettings();

        settings.Issuer.Should().BeEmpty();
        settings.Audience.Should().BeEmpty();
        settings.SigningKey.Should().BeEmpty();
        settings.AccessTokenExpirationMinutes.Should().Be(60);
    }

    [Fact]
    public void Should_BindFromConfiguration()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:Issuer"] = "issuer",
                ["JwtSettings:Audience"] = "audience",
                ["JwtSettings:SigningKey"] = "signing-key",
                ["JwtSettings:AccessTokenExpirationMinutes"] = "30",
            })
            .Build();

        JwtSettings? settings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>();

        settings.Should().NotBeNull();
        settings!.Issuer.Should().Be("issuer");
        settings.Audience.Should().Be("audience");
        settings.SigningKey.Should().Be("signing-key");
        settings.AccessTokenExpirationMinutes.Should().Be(30);
    }
}

using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Reflection;
using WebApi.Infrastructure;

namespace WebApi.Tests.Unit.Infrastructure;

public class VersionInfoProviderTests
{
    private static readonly Assembly Assembly = typeof(VersionInfoProvider).Assembly;

    [Theory]
    [InlineData("9.9.9+abcdef123", "9.9.9", "abcdef123")]
    [InlineData("2.3.0", "2.3.0", "")]
    [InlineData("2.3.0+abc-dirty", "2.3.0", "abc-dirty")]
    public void ParseInformationalVersion_Should_Split_Version_And_Commit(
        string informationalVersion,
        string expectedVersion,
        string expectedCommit)
    {
        (string version, string commit) = VersionInfoProvider.ParseInformationalVersion(informationalVersion);

        version.Should().Be(expectedVersion);
        commit.Should().Be(expectedCommit);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ParseInformationalVersion_Should_Fall_Back_When_Missing(string? informationalVersion)
    {
        (string version, string commit) = VersionInfoProvider.ParseInformationalVersion(informationalVersion);

        version.Should().Be("0.0.0");
        commit.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_Should_Report_The_Host_Environment()
    {
        VersionInfoProvider sut = CreateProvider("Testing");

        sut.EnvironmentName.Should().Be("Testing");
    }

    [Fact]
    public void Constructor_Should_Report_The_Running_Runtime()
    {
        VersionInfoProvider sut = CreateProvider();

        sut.RuntimeVersion.Should().Be(Environment.Version.ToString());
        sut.RuntimeMajor.Should().Be(Environment.Version.Major);
    }

    [Fact]
    public void Constructor_Should_Report_The_Assembly_Timestamp_As_Build_Date()
    {
        VersionInfoProvider sut = CreateProvider();

        sut.BuildDate.Should().Be(File.GetLastWriteTimeUtc(Assembly.Location));
    }

    [Fact]
    public void Constructor_Should_Read_The_Informational_Version_Of_The_Given_Assembly()
    {
        string informationalVersion = Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
            .InformationalVersion;
        (string expectedVersion, string expectedCommit) = VersionInfoProvider.ParseInformationalVersion(informationalVersion);

        VersionInfoProvider sut = CreateProvider();

        sut.Version.Should().Be(expectedVersion);
        sut.Commit.Should().Be(expectedCommit);
    }

    private static VersionInfoProvider CreateProvider(string environmentName = "Production")
        => new(new FakeHostEnvironment(environmentName), Assembly);

    private sealed class FakeHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "WebApi.Tests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}

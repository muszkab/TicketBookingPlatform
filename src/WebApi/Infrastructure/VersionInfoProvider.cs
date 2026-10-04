using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Reflection;

namespace WebApi.Infrastructure;

public sealed class VersionInfoProvider
{
    private const string FallbackVersion = "0.0.0";

    public VersionInfoProvider(IHostEnvironment environment)
        : this(environment, Assembly.GetEntryAssembly() ?? typeof(VersionInfoProvider).Assembly)
    {
    }

    public VersionInfoProvider(IHostEnvironment environment, Assembly assembly)
    {
        (string version, string commit) = ParseInformationalVersion(ReadInformationalVersion(assembly));

        Version = version;
        Commit = commit;
        RuntimeVersion = Environment.Version.ToString();
        RuntimeMajor = Environment.Version.Major;
        BuildDate = ReadBuildDate(assembly);
        EnvironmentName = environment.EnvironmentName;
    }

    public string Version { get; }

    public string Commit { get; }

    public string RuntimeVersion { get; }

    public int RuntimeMajor { get; }

    public DateTimeOffset? BuildDate { get; }

    public string EnvironmentName { get; }

    public static (string Version, string Commit) ParseInformationalVersion(string? informationalVersion)
    {
        if (string.IsNullOrWhiteSpace(informationalVersion))
        {
            return (FallbackVersion, string.Empty);
        }

        int separator = informationalVersion.IndexOf('+', StringComparison.Ordinal);

        return separator < 0
            ? (informationalVersion, string.Empty)
            : (informationalVersion[..separator], informationalVersion[(separator + 1)..]);
    }

    private static string? ReadInformationalVersion(Assembly assembly)
        => assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

    // MSBuild embeds no build timestamp (deterministic builds deliberately avoid one), so the
    // assembly file's timestamp is the closest proxy: the publish/deploy time of this build.
    private static DateTimeOffset? ReadBuildDate(Assembly assembly)
    {
        string location = assembly.Location;

        return string.IsNullOrEmpty(location) || !File.Exists(location)
            ? null
            : File.GetLastWriteTimeUtc(location);
    }
}

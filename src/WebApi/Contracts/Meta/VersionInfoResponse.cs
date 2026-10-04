using System;

namespace WebApi.Contracts.Meta;

public sealed record VersionInfoResponse(
    string Version,
    string Commit,
    string RuntimeVersion,
    int RuntimeMajor,
    DateTimeOffset? BuildDate,
    string Environment);

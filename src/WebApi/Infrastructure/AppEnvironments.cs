using Microsoft.Extensions.Hosting;

namespace WebApi.Infrastructure;

public static class AppEnvironments
{
    public const string Testing = "Testing";
}

public static class AppEnvironmentExtensions
{
    public static bool IsTesting(this IHostEnvironment environment)
        => environment.IsEnvironment(AppEnvironments.Testing);
}

using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using System;

namespace Infrastructure.Configuration;

/// <summary>
/// Adds Azure Key Vault as a configuration source, authenticated with a Managed Identity.
/// </summary>
/// <remarks>
/// Opt-in: only registered when "KeyVault:Uri" ("KeyVault__Uri") is set, so local development and
/// Docker Compose keep using user secrets / environment variables. Secret names use "--" instead of
/// the ":" separator (e.g. "ConnectionStrings--DefaultConnection").
/// </remarks>
public static class KeyVaultConfigurationExtensions
{
    public static IConfigurationBuilder AddAzureKeyVault(
        this IConfigurationBuilder builder,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);

        string? vaultUri = configuration["KeyVault:Uri"];

        if (string.IsNullOrWhiteSpace(vaultUri))
        {
            return builder;
        }

        if (!Uri.TryCreate(vaultUri, UriKind.Absolute, out Uri? parsedVaultUri))
        {
            throw new InvalidOperationException(
                $"'KeyVault:Uri' must be an absolute URI, but was '{vaultUri}'.");
        }

        string? managedIdentityClientId = configuration["KeyVault:ManagedIdentityClientId"];

        DefaultAzureCredentialOptions credentialOptions = new();

        if (!string.IsNullOrWhiteSpace(managedIdentityClientId))
        {
            credentialOptions.ManagedIdentityClientId = managedIdentityClientId;
        }

        builder.AddAzureKeyVault(
            parsedVaultUri,
            new DefaultAzureCredential(credentialOptions),
            new AzureKeyVaultConfigurationOptions
            {
                ReloadInterval = TimeSpan.FromMinutes(30)
            });

        return builder;
    }
}

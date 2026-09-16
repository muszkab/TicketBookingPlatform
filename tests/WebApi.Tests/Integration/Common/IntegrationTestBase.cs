using Domain.Users;
using Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace WebApi.Tests.Integration.Common;

public abstract class IntegrationTestBase : IClassFixture<CustomWebApplicationFactory>
{
    protected static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    protected CustomWebApplicationFactory Factory { get; }

    protected IntegrationTestBase(CustomWebApplicationFactory factory)
    {
        Factory = factory;
        Factory.EnsureDatabaseCreated();
    }

    /// <summary>
    /// An anonymous client. BaseAddress uses https so UseHttpsRedirection does not
    /// turn every request into a 307.
    /// </summary>
    protected HttpClient CreateClient()
        => Factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
        });

    protected HttpClient CreateAuthenticatedClient(User user)
    {
        HttpClient client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", Factory.CreateAccessToken(user));
        return client;
    }

    protected HttpClient CreateClientWithRawToken(string token)
    {
        HttpClient client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>
    /// Runs an action against a fresh <see cref="ApplicationDbContext"/> scope.
    /// </summary>
    protected async Task ExecuteDbAsync(Func<ApplicationDbContext, Task> action)
    {
        using IServiceScope scope = Factory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await action(context);
    }

    protected async Task<TResult> ExecuteDbAsync<TResult>(Func<ApplicationDbContext, Task<TResult>> action)
    {
        using IServiceScope scope = Factory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await action(context);
    }

    protected static async Task<TResponse?> ReadAsAsync<TResponse>(HttpResponseMessage response)
    {
        string json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<TResponse>(json, JsonOptions);
    }
}

using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;

namespace Halifax.Http;

/// <summary>
/// Halifax ServiceCollection AddHalifaxHttpClient extensions for easy HTTP client registration
/// </summary>
public static class HalifaxHttpClientExtensions
{
    /// <summary>Registers a typed HTTP client with a base URL.</summary>
    public static IServiceCollection AddHalifaxHttpClient<THalifaxHttpClient>(
        this IServiceCollection services,
        string defaultBaseUrl) where THalifaxHttpClient : HalifaxHttpClient
    {
        services.AddHalifaxHttpClientBuilder<THalifaxHttpClient>(
            defaultBaseUrl,
            defaultBearerToken: null,
            configure: null);
        return services;
    }

    /// <summary>Registers a typed HTTP client with a base URL and client configuration action.</summary>
    public static IServiceCollection AddHalifaxHttpClient<THalifaxHttpClient>(
        this IServiceCollection services,
        string defaultBaseUrl,
        Action<HttpClient>? configure) where THalifaxHttpClient : HalifaxHttpClient
    {
        services.AddHalifaxHttpClientBuilder<THalifaxHttpClient>(
            defaultBaseUrl,
            null,
            configure != null ? (_, client) => configure(client) : null);
        return services;
    }

    /// <summary>Registers a typed HTTP client with a base URL and service-provider-aware configuration action.</summary>
    public static IServiceCollection AddHalifaxHttpClient<THalifaxHttpClient>(
        this IServiceCollection services,
        string defaultBaseUrl,
        Action<IServiceProvider, HttpClient>? configure) where THalifaxHttpClient : HalifaxHttpClient
    {
        services.AddHalifaxHttpClientBuilder<THalifaxHttpClient>(
            defaultBaseUrl,
            null,
            configure);
        return services;
    }

    /// <summary>Registers a typed HTTP client with a base URL and optional bearer token.</summary>
    public static IServiceCollection AddHalifaxHttpClient<THalifaxHttpClient>(
        this IServiceCollection services,
        string defaultBaseUrl,
        string? defaultBearerToken) where THalifaxHttpClient : HalifaxHttpClient
    {
        services.AddHalifaxHttpClientBuilder<THalifaxHttpClient>(
            defaultBaseUrl,
            defaultBearerToken,
            configure: null);
        return services;
    }

    /// <summary>Registers a typed HTTP client with a base URL, optional bearer token, and client configuration action.</summary>
    public static IServiceCollection AddHalifaxHttpClient<THalifaxHttpClient>(
        this IServiceCollection services,
        string defaultBaseUrl,
        string? defaultBearerToken,
        Action<HttpClient>? configure) where THalifaxHttpClient : HalifaxHttpClient
    {
        services.AddHalifaxHttpClientBuilder<THalifaxHttpClient>(
            defaultBaseUrl,
            defaultBearerToken,
            configure != null ? (_, client) => configure(client) : null);
        return services;
    }

    /// <summary>Registers a typed HTTP client with a base URL, optional bearer token, and service-provider-aware configuration.</summary>
    public static IServiceCollection AddHalifaxHttpClient<THalifaxHttpClient>(
        this IServiceCollection services,
        string defaultBaseUrl,
        string? defaultBearerToken,
        Action<IServiceProvider, HttpClient>? configure) where THalifaxHttpClient : HalifaxHttpClient
    {
        services.AddHalifaxHttpClientBuilder<THalifaxHttpClient>(
            defaultBaseUrl,
            defaultBearerToken,
            configure);
        return services;
    }

    /// <summary>
    /// Registers a typed HTTP client and returns the <see cref="IHttpClientBuilder"/> for further configuration (e.g. adding handlers).
    /// </summary>
    public static IHttpClientBuilder AddHalifaxHttpClientBuilder<THalifaxHttpClient>(
        this IServiceCollection services,
        string defaultBaseUrl,
        string? defaultBearerToken,
        Action<IServiceProvider, HttpClient>? configure) where THalifaxHttpClient : HalifaxHttpClient
    {
        return services.AddHttpClient<THalifaxHttpClient>((provider, client) =>
        {
            client.BaseAddress = new Uri(defaultBaseUrl);

            if (!string.IsNullOrWhiteSpace(defaultBearerToken))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", defaultBearerToken);
            }

            configure?.Invoke(provider, client);
        });
    }

    /// <summary>
    /// Registers a typed HTTP client with the standard resilience handler (retries, circuit breaker, timeouts).
    /// </summary>
    public static IHttpClientBuilder AddHalifaxHttpClientWithResilience<THalifaxHttpClient>(
        this IServiceCollection services,
        string defaultBaseUrl,
        string? defaultBearerToken = null,
        Action<IServiceProvider, HttpClient>? configure = null) where THalifaxHttpClient : HalifaxHttpClient
    {
        var builder = services.AddHalifaxHttpClientBuilder<THalifaxHttpClient>(
            defaultBaseUrl,
            defaultBearerToken,
            configure);
        builder.AddStandardResilienceHandler();
        return builder;
    }
}

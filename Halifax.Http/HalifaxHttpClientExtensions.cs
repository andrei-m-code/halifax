using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;

namespace Halifax.Http;

/// <summary>
/// Halifax ServiceCollection AddHalifaxHttpClient extensions for easy HTTP client registration
/// </summary>
public static class HalifaxHttpClientExtensions
{
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

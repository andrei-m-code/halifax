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
        return services.AddHalifaxHttpClient<THalifaxHttpClient>(
            defaultBaseUrl, 
            defaultBearerToken: null, 
            (Action<IServiceProvider, HttpClient>) null);
    }
    
    public static IServiceCollection AddHalifaxHttpClient<THalifaxHttpClient>(
        this IServiceCollection services, 
        string defaultBaseUrl, 
        Action<HttpClient> configure) where THalifaxHttpClient : HalifaxHttpClient
    {
        return services.AddHalifaxHttpClient<THalifaxHttpClient>(
            defaultBaseUrl, 
            null, 
            (_, client) => configure?.Invoke(client));
    }
    
    public static IServiceCollection AddHalifaxHttpClient<THalifaxHttpClient>(
        this IServiceCollection services, 
        string defaultBaseUrl, 
        Action<IServiceProvider, HttpClient> configure) where THalifaxHttpClient : HalifaxHttpClient
    {
        return services.AddHalifaxHttpClient<THalifaxHttpClient>(
            defaultBaseUrl, 
            null, 
            (provider, client) => configure?.Invoke(provider, client));
    }

    public static IServiceCollection AddHalifaxHttpClient<THalifaxHttpClient>(
        this IServiceCollection services, 
        string defaultBaseUrl, 
        string defaultBearerToken) where THalifaxHttpClient : HalifaxHttpClient
    {
        return services.AddHalifaxHttpClient<THalifaxHttpClient>(
            defaultBaseUrl, 
            defaultBearerToken, 
            (Action<IServiceProvider, HttpClient>) null);
    }
    
    public static IServiceCollection AddHalifaxHttpClient<THalifaxHttpClient>(
        this IServiceCollection services, 
        string defaultBaseUrl, 
        string defaultBearerToken, 
        Action<HttpClient> configure) where THalifaxHttpClient : HalifaxHttpClient
    {
        return services.AddHalifaxHttpClient<THalifaxHttpClient>(
            defaultBaseUrl, 
            defaultBearerToken, 
            (_, client) => configure?.Invoke(client));
    }
    
    public static IServiceCollection AddHalifaxHttpClient<THalifaxHttpClient>(
        this IServiceCollection services, 
        string defaultBaseUrl, 
        string defaultBearerToken, 
        Action<IServiceProvider, HttpClient> configure) where THalifaxHttpClient : HalifaxHttpClient
    {
        services.AddHttpClient<THalifaxHttpClient>((provider, client) =>
        {
            client.BaseAddress = new Uri(defaultBaseUrl);

            if (!string.IsNullOrWhiteSpace(defaultBearerToken))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", defaultBearerToken);
            }

            configure?.Invoke(provider, client);
        });

        return services;
    }
}
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;

namespace Halifax.Http;

/// <summary>
/// <see cref="IServiceCollection"/> extension methods for registering typed
/// <see cref="HalifaxHttpClient"/> implementations with the dependency injection container.
/// </summary>
/// <remarks>
/// These methods wrap <see cref="HttpClientFactoryServiceCollectionExtensions.AddHttpClient{TClient}(IServiceCollection)"/>
/// and configure the client's base address and, optionally, a default bearer token before invoking any
/// caller-supplied configuration. Use <see cref="AddHalifaxHttpClientWithResilience{THalifaxHttpClient}"/>
/// to also attach the standard resilience handler, or <see cref="AddHalifaxHttpClientBuilder{THalifaxHttpClient}"/>
/// to obtain the <see cref="IHttpClientBuilder"/> for further customization such as adding delegating handlers.
/// </remarks>
public static class HalifaxHttpClientExtensions
{
    /// <summary>Registers the typed HTTP client <typeparamref name="THalifaxHttpClient"/> with a base URL.</summary>
    /// <typeparam name="THalifaxHttpClient">The <see cref="HalifaxHttpClient"/> implementation to register.</typeparam>
    /// <param name="services">The service collection to add the client to.</param>
    /// <param name="defaultBaseUrl">The base URL applied to the client's <see cref="System.Net.Http.HttpClient.BaseAddress"/>.</param>
    /// <returns>The same <paramref name="services"/> instance so that calls can be chained.</returns>
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

    /// <summary>Registers the typed HTTP client <typeparamref name="THalifaxHttpClient"/> with a base URL and a client configuration action.</summary>
    /// <typeparam name="THalifaxHttpClient">The <see cref="HalifaxHttpClient"/> implementation to register.</typeparam>
    /// <param name="services">The service collection to add the client to.</param>
    /// <param name="defaultBaseUrl">The base URL applied to the client's <see cref="System.Net.Http.HttpClient.BaseAddress"/>.</param>
    /// <param name="configure">
    /// An optional action to further configure the <see cref="System.Net.Http.HttpClient"/>, invoked after the
    /// base address is set, or <see langword="null"/> for no additional configuration.
    /// </param>
    /// <returns>The same <paramref name="services"/> instance so that calls can be chained.</returns>
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

    /// <summary>Registers the typed HTTP client <typeparamref name="THalifaxHttpClient"/> with a base URL and a service-provider-aware configuration action.</summary>
    /// <typeparam name="THalifaxHttpClient">The <see cref="HalifaxHttpClient"/> implementation to register.</typeparam>
    /// <param name="services">The service collection to add the client to.</param>
    /// <param name="defaultBaseUrl">The base URL applied to the client's <see cref="System.Net.Http.HttpClient.BaseAddress"/>.</param>
    /// <param name="configure">
    /// An optional action to further configure the <see cref="System.Net.Http.HttpClient"/> with access to the
    /// <see cref="IServiceProvider"/>, invoked after the base address is set, or <see langword="null"/> for no
    /// additional configuration.
    /// </param>
    /// <returns>The same <paramref name="services"/> instance so that calls can be chained.</returns>
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

    /// <summary>Registers the typed HTTP client <typeparamref name="THalifaxHttpClient"/> with a base URL and an optional default bearer token.</summary>
    /// <typeparam name="THalifaxHttpClient">The <see cref="HalifaxHttpClient"/> implementation to register.</typeparam>
    /// <param name="services">The service collection to add the client to.</param>
    /// <param name="defaultBaseUrl">The base URL applied to the client's <see cref="System.Net.Http.HttpClient.BaseAddress"/>.</param>
    /// <param name="defaultBearerToken">
    /// An optional bearer token added as a default <c>Authorization</c> header; when <see langword="null"/> or
    /// whitespace, no authorization header is set.
    /// </param>
    /// <returns>The same <paramref name="services"/> instance so that calls can be chained.</returns>
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

    /// <summary>Registers the typed HTTP client <typeparamref name="THalifaxHttpClient"/> with a base URL, an optional default bearer token, and a client configuration action.</summary>
    /// <typeparam name="THalifaxHttpClient">The <see cref="HalifaxHttpClient"/> implementation to register.</typeparam>
    /// <param name="services">The service collection to add the client to.</param>
    /// <param name="defaultBaseUrl">The base URL applied to the client's <see cref="System.Net.Http.HttpClient.BaseAddress"/>.</param>
    /// <param name="defaultBearerToken">
    /// An optional bearer token added as a default <c>Authorization</c> header; when <see langword="null"/> or
    /// whitespace, no authorization header is set.
    /// </param>
    /// <param name="configure">
    /// An optional action to further configure the <see cref="System.Net.Http.HttpClient"/>, invoked after the
    /// base address and authorization header are set, or <see langword="null"/> for no additional configuration.
    /// </param>
    /// <returns>The same <paramref name="services"/> instance so that calls can be chained.</returns>
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

    /// <summary>Registers the typed HTTP client <typeparamref name="THalifaxHttpClient"/> with a base URL, an optional default bearer token, and a service-provider-aware configuration action.</summary>
    /// <typeparam name="THalifaxHttpClient">The <see cref="HalifaxHttpClient"/> implementation to register.</typeparam>
    /// <param name="services">The service collection to add the client to.</param>
    /// <param name="defaultBaseUrl">The base URL applied to the client's <see cref="System.Net.Http.HttpClient.BaseAddress"/>.</param>
    /// <param name="defaultBearerToken">
    /// An optional bearer token added as a default <c>Authorization</c> header; when <see langword="null"/> or
    /// whitespace, no authorization header is set.
    /// </param>
    /// <param name="configure">
    /// An optional action to further configure the <see cref="System.Net.Http.HttpClient"/> with access to the
    /// <see cref="IServiceProvider"/>, invoked after the base address and authorization header are set, or
    /// <see langword="null"/> for no additional configuration.
    /// </param>
    /// <returns>The same <paramref name="services"/> instance so that calls can be chained.</returns>
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
    /// Registers the typed HTTP client <typeparamref name="THalifaxHttpClient"/> and returns the
    /// <see cref="IHttpClientBuilder"/> for further configuration such as adding delegating handlers.
    /// </summary>
    /// <typeparam name="THalifaxHttpClient">The <see cref="HalifaxHttpClient"/> implementation to register.</typeparam>
    /// <param name="services">The service collection to add the client to.</param>
    /// <param name="defaultBaseUrl">The base URL applied to the client's <see cref="System.Net.Http.HttpClient.BaseAddress"/>.</param>
    /// <param name="defaultBearerToken">
    /// An optional bearer token added as a default <c>Authorization</c> header; when <see langword="null"/> or
    /// whitespace, no authorization header is set.
    /// </param>
    /// <param name="configure">
    /// An optional action to further configure the <see cref="System.Net.Http.HttpClient"/> with access to the
    /// <see cref="IServiceProvider"/>, invoked after the base address and authorization header are set, or
    /// <see langword="null"/> for no additional configuration.
    /// </param>
    /// <returns>The <see cref="IHttpClientBuilder"/> for the registered client, enabling further chaining.</returns>
    /// <remarks>
    /// This is the underlying builder that all other <c>AddHalifaxHttpClient</c> overloads delegate to. The
    /// base address is set from <paramref name="defaultBaseUrl"/> and, when supplied, a <c>Bearer</c>
    /// authorization header from <paramref name="defaultBearerToken"/>, before <paramref name="configure"/>
    /// runs. Use the returned builder to attach handlers such as <see cref="CorrelationIdDelegatingHandler"/>
    /// or resilience policies.
    /// </remarks>
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
    /// Registers the typed HTTP client <typeparamref name="THalifaxHttpClient"/> and attaches the standard
    /// resilience handler (retries, circuit breaker, and timeouts).
    /// </summary>
    /// <typeparam name="THalifaxHttpClient">The <see cref="HalifaxHttpClient"/> implementation to register.</typeparam>
    /// <param name="services">The service collection to add the client to.</param>
    /// <param name="defaultBaseUrl">The base URL applied to the client's <see cref="System.Net.Http.HttpClient.BaseAddress"/>.</param>
    /// <param name="defaultBearerToken">
    /// An optional bearer token added as a default <c>Authorization</c> header; when <see langword="null"/> or
    /// whitespace, no authorization header is set.
    /// </param>
    /// <param name="configure">
    /// An optional action to further configure the <see cref="System.Net.Http.HttpClient"/> with access to the
    /// <see cref="IServiceProvider"/>, invoked after the base address and authorization header are set, or
    /// <see langword="null"/> for no additional configuration.
    /// </param>
    /// <returns>The <see cref="IHttpClientBuilder"/> for the registered client, enabling further chaining.</returns>
    /// <remarks>
    /// This is equivalent to <see cref="AddHalifaxHttpClientBuilder{THalifaxHttpClient}"/> followed by a call to
    /// <c>AddStandardResilienceHandler</c>, which applies the framework's default resilience pipeline.
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddHalifaxHttpClientWithResilience&lt;UsersClient&gt;(
    ///     defaultBaseUrl: "https://api.example.com",
    ///     defaultBearerToken: token);
    /// </code>
    /// </example>
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

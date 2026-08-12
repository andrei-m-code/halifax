using Halifax.Api.App.Defaults;
using Halifax.Core.Helpers;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text;
using System.Text.Json;
using Halifax.Core;

namespace Halifax.Api.App;

/// <summary>
/// Fluent builder for configuring a Halifax API application. An instance is created and passed to the
/// callback supplied to <see cref="AppExtensions.AddHalifax"/>; each configuration method returns the same
/// builder so calls can be chained.
/// </summary>
/// <remarks>
/// Only one builder may exist per application; attempting to initialize Halifax more than once throws.
/// Every method captures its argument for later use by <see cref="AppExtensions.AddHalifax"/> and
/// <see cref="AppExtensions.UseHalifax"/> — nothing is applied until those extension methods run.
/// </remarks>
/// <example>
/// <code>
/// services.AddHalifax(halifax => halifax
///     .SetName("Orders API")
///     .ConfigureAuthentication(jwtSecret)
///     .ConfigureCors(cors => cors.WithOrigins("https://example.com"))
///     .AddSettings&lt;OrdersSettings&gt;());
/// </code>
/// </example>
public class HalifaxBuilder
{
    internal static HalifaxBuilder? Instance { get; set; }

    private IServiceCollection Services { get;  }
    
    internal HalifaxBuilder(IServiceCollection services)
    {
        if (Instance != null)
        {
            throw new InvalidOperationException("Halifax app can only be initialized once");
        }

        Services = services;
        Instance = this;
    }

    internal string Name { get; private set; } = AppDomain.CurrentDomain.FriendlyName;
    
    internal Action<CorsPolicyBuilder> Cors { get; private set; } = CorsDefaults.Value;
    internal Action<SwaggerGenOptions> OpenApi { get; private set; } = OpenApiDefaults.Value;
    internal TokenValidationParameters? TokenValidationParameters { get; set; }

    internal bool useDefaultExceptionHandler = true;
    internal Action<JsonSerializerOptions> ConfigureJsonOptions { get; set; } = Json.ConfigureOptions;
    internal Action<IMvcBuilder> ConfigureMvcBuilder { get; set; } = opts => { };

    /// <summary>Sets the application name displayed in the OpenAPI document and the Scalar API reference title.</summary>
    /// <param name="name">The application name. Defaults to the current app domain's friendly name when not set.</param>
    /// <returns>The same <see cref="HalifaxBuilder"/> instance so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
    public HalifaxBuilder SetName(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        return this;
    }

    /// <summary>Configures the CORS policy applied by <see cref="AppExtensions.UseHalifax"/>.</summary>
    /// <param name="corsPolicyBuilder">
    /// Callback that builds the CORS policy. Replaces the Halifax default, which allows any header, method,
    /// and origin.
    /// </param>
    /// <returns>The same <see cref="HalifaxBuilder"/> instance so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="corsPolicyBuilder"/> is <see langword="null"/>.</exception>
    public HalifaxBuilder ConfigureCors(Action<CorsPolicyBuilder> corsPolicyBuilder)
    {
        Cors = corsPolicyBuilder ?? throw new ArgumentNullException(nameof(corsPolicyBuilder));
        return this;
    }

    /// <summary>Configures OpenAPI/Swagger generation options, replacing the Halifax defaults.</summary>
    /// <param name="openApiBuilder">
    /// Callback that configures Swagger generation. Overrides the default that registers a single "v1"
    /// document, adds a JWT Bearer security definition when authentication is configured, and includes XML
    /// comment files found in the output directory.
    /// </param>
    /// <returns>The same <see cref="HalifaxBuilder"/> instance so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="openApiBuilder"/> is <see langword="null"/>.</exception>
    public HalifaxBuilder ConfigureOpenApi(Action<SwaggerGenOptions> openApiBuilder)
    {
        OpenApi = openApiBuilder ?? throw new ArgumentNullException(nameof(openApiBuilder));
        return this;
    }

    /// <summary>
    /// Configures JWT Bearer authentication using a symmetric signing key derived from the supplied secret.
    /// </summary>
    /// <param name="jwtSecret">The shared secret used to build the symmetric signing key (UTF-8 encoded).</param>
    /// <param name="validateAudience">Whether to validate the token audience. Defaults to <see langword="false"/>.</param>
    /// <param name="validateIssuer">Whether to validate the token issuer. Defaults to <see langword="false"/>.</param>
    /// <param name="requireExpirationTime">Whether tokens must contain an expiration time. Defaults to <see langword="false"/>.</param>
    /// <returns>The same <see cref="HalifaxBuilder"/> instance so calls can be chained.</returns>
    /// <remarks>
    /// Calling any <c>ConfigureAuthentication</c> overload causes <see cref="AppExtensions.AddHalifax"/> to
    /// register JWT Bearer authentication and <see cref="AppExtensions.UseHalifax"/> to add the authentication
    /// and authorization middleware. Issuer signing key validation is always enabled.
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddHalifax(halifax => halifax
    ///     .ConfigureAuthentication(Env.Get("JWT_SECRET")));
    /// </code>
    /// </example>
    public HalifaxBuilder ConfigureAuthentication(string jwtSecret,
        bool validateAudience = false,
        bool validateIssuer = false,
        bool requireExpirationTime = false)
    {
        return ConfigureAuthentication(new TokenValidationParameters
        {
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuerSigningKey = true,
            ValidateAudience = validateAudience,
            ValidateIssuer = validateIssuer,
            RequireExpirationTime = requireExpirationTime
        });
    }

    /// <summary>
    /// Configures JWT Bearer authentication using fully custom token validation parameters.
    /// </summary>
    /// <param name="parameters">The token validation parameters governing how incoming JWTs are validated.</param>
    /// <returns>The same <see cref="HalifaxBuilder"/> instance so calls can be chained.</returns>
    /// <remarks>
    /// Supplying <paramref name="parameters"/> opts the application in to authentication: the JWT Bearer
    /// handler and the authentication/authorization middleware are added during Halifax setup.
    /// </remarks>
    public HalifaxBuilder ConfigureAuthentication(TokenValidationParameters parameters)
    {
        TokenValidationParameters = parameters;
        return this;
    }

    /// <summary>Configures whether the default <see cref="Errors.HalifaxExceptionHandler"/> is registered.</summary>
    /// <param name="useDefaultHalifaxExceptionHandler">
    /// <see langword="true"/> (the default) to register the built-in handler that maps Halifax exceptions to
    /// HTTP status codes; <see langword="false"/> to opt out and supply your own exception handling.
    /// </param>
    /// <returns>The same <see cref="HalifaxBuilder"/> instance so calls can be chained.</returns>
    public HalifaxBuilder ConfigureExceptionHandler(bool useDefaultHalifaxExceptionHandler = true)
    {
        this.useDefaultExceptionHandler = useDefaultHalifaxExceptionHandler; 
        return this;
    }

    /// <summary>Configures the JSON serializer options used by both the MVC pipeline and the global Halifax serializer.</summary>
    /// <param name="configure">Callback that mutates the <see cref="JsonSerializerOptions"/>, replacing the Halifax defaults.</param>
    /// <returns>The same <see cref="HalifaxBuilder"/> instance so calls can be chained.</returns>
    public HalifaxBuilder ConfigureJson(Action<JsonSerializerOptions> configure)
    {
        ConfigureJsonOptions = configure;
        return this;
    }

    /// <summary>Configures the underlying <see cref="IMvcBuilder"/> for additional controller, convention, or formatter setup.</summary>
    /// <param name="configure">Callback invoked with the MVC builder after Halifax registers its controllers and JSON options.</param>
    /// <returns>The same <see cref="HalifaxBuilder"/> instance so calls can be chained.</returns>
    public HalifaxBuilder ConfigureMvc(Action<IMvcBuilder> configure)
    {
        ConfigureMvcBuilder = configure;
        return this;
    }

    /// <summary>
    /// Binds a settings class from environment variables and registers the resulting instance as a singleton.
    /// </summary>
    /// <typeparam name="TSettings">The settings type to bind and register.</typeparam>
    /// <returns>The same <see cref="HalifaxBuilder"/> instance so calls can be chained.</returns>
    /// <remarks>
    /// The settings instance is populated by <see cref="Env.GetSection{TSettings}"/> from the loaded
    /// environment configuration. Use the <see cref="AddSettings{TSettings}(out TSettings)"/> overload to also
    /// obtain the bound instance during configuration.
    /// </remarks>
    public HalifaxBuilder AddSettings<TSettings>() where TSettings : class
    {
        return AddSettings<TSettings>(out _);
    }

    /// <summary>
    /// Binds a settings class from environment variables, registers it as a singleton, and outputs the bound instance.
    /// </summary>
    /// <typeparam name="TSettings">The settings type to bind and register.</typeparam>
    /// <param name="settings">When this method returns, contains the settings instance bound from environment variables.</param>
    /// <returns>The same <see cref="HalifaxBuilder"/> instance so calls can be chained.</returns>
    public HalifaxBuilder AddSettings<TSettings>(out TSettings settings) where TSettings : class
    {
        settings = Env.GetSection<TSettings>();
        Services.AddSingleton(settings);
        
        return this;
    }
}

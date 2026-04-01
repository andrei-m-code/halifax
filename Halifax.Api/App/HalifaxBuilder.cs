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
/// Fluent builder for configuring a Halifax API application.
/// </summary>
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

    /// <summary>Sets the application name displayed in OpenAPI documentation.</summary>
    public HalifaxBuilder SetName(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        return this;
    }

    /// <summary>Configures the CORS policy.</summary>
    public HalifaxBuilder ConfigureCors(Action<CorsPolicyBuilder> corsPolicyBuilder)
    {
        Cors = corsPolicyBuilder ?? throw new ArgumentNullException(nameof(corsPolicyBuilder));
        return this;
    }

    /// <summary>Configures OpenAPI/Swagger generation options.</summary>
    public HalifaxBuilder ConfigureOpenApi(Action<SwaggerGenOptions> openApiBuilder)
    {
        OpenApi = openApiBuilder ?? throw new ArgumentNullException(nameof(openApiBuilder));
        return this;
    }

    /// <summary>Configures JWT Bearer authentication with a symmetric secret key.</summary>
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

    /// <summary>Configures JWT Bearer authentication with custom token validation parameters.</summary>
    public HalifaxBuilder ConfigureAuthentication(TokenValidationParameters parameters)
    {
        TokenValidationParameters = parameters;
        return this;
    }

    /// <summary>Configures whether to use the default Halifax exception handler.</summary>
    public HalifaxBuilder ConfigureExceptionHandler(bool useDefaultHalifaxExceptionHandler = true)
    {
        this.useDefaultExceptionHandler = useDefaultHalifaxExceptionHandler; 
        return this;
    }

    /// <summary>Configures JSON serializer options for the API.</summary>
    public HalifaxBuilder ConfigureJson(Action<JsonSerializerOptions> configure)
    {
        ConfigureJsonOptions = configure;
        return this;
    }

    /// <summary>Configures the MVC builder for additional controller or formatter setup.</summary>
    public HalifaxBuilder ConfigureMvc(Action<IMvcBuilder> configure)
    {
        ConfigureMvcBuilder = configure;
        return this;
    }

    /// <summary>Registers a settings class as a singleton, loaded from environment variables.</summary>
    public HalifaxBuilder AddSettings<TSettings>() where TSettings : class
    {
        return AddSettings<TSettings>(out _);
    }

    /// <summary>Registers a settings class as a singleton, loaded from environment variables, and outputs the instance.</summary>
    public HalifaxBuilder AddSettings<TSettings>(out TSettings settings) where TSettings : class
    {
        settings = Env.GetSection<TSettings>();
        Services.AddSingleton(settings);
        
        return this;
    }
}

using Halifax.Api.App.Defaults;
using Halifax.Api.Errors;
using Halifax.Core.Helpers;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text;
using System.Text.Json;
using Halifax.Core;

namespace Halifax.Api.App;

public class HalifaxBuilder
{
    internal static HalifaxBuilder Instance { get; set; }

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
    internal Action<SwaggerGenOptions> Swagger { get; private set; } = SwaggerDefaults.Value;
    internal TokenValidationParameters TokenValidationParameters { get; set; }
    internal Type ExceptionHandlerType { get; set; } = typeof(HalifaxExceptionHandler);
    internal Action<JsonSerializerOptions> ConfigureJsonOptions { get; set; } = Json.ConfigureOptions;
    internal Action<IMvcBuilder> ConfigureMvcBuilder { get; set; } = opts => { };

    public HalifaxBuilder SetName(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        return this;
    }

    public HalifaxBuilder ConfigureCors(Action<CorsPolicyBuilder> corsPolicyBuilder)
    {
        Cors = corsPolicyBuilder ?? throw new ArgumentNullException(nameof(corsPolicyBuilder));
        return this;
    }

    public HalifaxBuilder ConfigureSwagger(Action<SwaggerGenOptions> swaggerBuilder)
    {
        Swagger = swaggerBuilder ?? throw new ArgumentNullException(nameof(swaggerBuilder));
        return this;
    }

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

    public HalifaxBuilder ConfigureAuthentication(TokenValidationParameters parameters)
    {
        TokenValidationParameters = parameters;
        return this;
    }

    public HalifaxBuilder ConfigureExceptionHandler<TExceptionHandler>() where TExceptionHandler : IExceptionHandler
    {
        ExceptionHandlerType = typeof(TExceptionHandler);
        return this;
    }

    public HalifaxBuilder ConfigureJson(Action<JsonSerializerOptions> configure)
    {
        ConfigureJsonOptions = configure;
        return this;
    }

    public HalifaxBuilder ConfigureMvc(Action<IMvcBuilder> configure)
    {
        ConfigureMvcBuilder = configure;
        return this;
    }

    public HalifaxBuilder AddSettings<TSettings>() where TSettings : class
    {
        return AddSettings<TSettings>(out _);
    }
    
    public HalifaxBuilder AddSettings<TSettings>(out TSettings settings) where TSettings : class
    {
        settings = Env.GetSection<TSettings>();
        Services.AddSingleton(settings);
        
        return this;
    }
}

using Halifax.Api.App;
using Halifax.Api.Errors;
using Halifax.Api.Middleware;
using Halifax.Core;
using Halifax.Core.Helpers;
using Halifax.Domain.Exceptions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Scalar.AspNetCore;

namespace Halifax.Api;

/// <summary>
/// Extension methods for registering and configuring Halifax API services and middleware.
/// </summary>
public static class AppExtensions
{
    /// <summary>
    /// Registers all Halifax services into the dependency injection container: controllers with Halifax
    /// JSON options, CORS, OpenAPI/Swagger generation, optional JWT Bearer authentication, and the default
    /// exception handler.
    /// </summary>
    /// <param name="services">The service collection to add the Halifax services to.</param>
    /// <param name="configure">
    /// Optional callback that receives a <see cref="HalifaxBuilder"/> for customizing the configuration
    /// (application name, authentication, CORS, OpenAPI, JSON, MVC, and settings registration). When
    /// <see langword="null"/>, Halifax is registered with its defaults.
    /// </param>
    /// <remarks>
    /// This method performs the service-registration half of Halifax setup and must be paired with
    /// <see cref="UseHalifax"/> in the middleware pipeline. It resets the default logging providers, loads
    /// <c>.env</c> configuration via <see cref="Env.Load"/>, and applies the configured JSON options to both
    /// the MVC pipeline and the global <see cref="Json"/> serializer. JWT Bearer authentication is only added
    /// when <see cref="HalifaxBuilder.ConfigureAuthentication(Microsoft.IdentityModel.Tokens.TokenValidationParameters)"/>
    /// (or one of its overloads) was called; failed challenges surface as a
    /// <see cref="HalifaxUnauthorizedException"/>. The
    /// default <see cref="HalifaxExceptionHandler"/> is registered unless disabled via
    /// <see cref="HalifaxBuilder.ConfigureExceptionHandler"/>.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown by the <see cref="HalifaxBuilder"/> constructor if Halifax has already been initialized in the
    /// current application (it may only be set up once).
    /// </exception>
    /// <example>
    /// <code>
    /// builder.Services.AddHalifax(halifax => halifax
    ///     .SetName("My API")
    ///     .ConfigureAuthentication(jwtSecret)
    ///     .AddSettings&lt;MySettings&gt;());
    /// </code>
    /// </example>
    public static void AddHalifax(this IServiceCollection services, Action<HalifaxBuilder>? configure = null)
    {
        services.CleanupDefaultLogging();

        L.Info("Starting up Halifax");

        // Load .env configuration
        Env.Load();

        var builder = new HalifaxBuilder(services);
        configure?.Invoke(builder);

        Json.ConfigureOptions = builder.ConfigureJsonOptions;

        var mvcBuilder = services
            .AddControllers()
            .AddJsonOptions(options => Json.ConfigureOptions(options.JsonSerializerOptions))
            .AddApplicationPart(typeof(AppExtensions).Assembly);

        builder.ConfigureMvcBuilder(mvcBuilder);

        if (builder.TokenValidationParameters != null)
        {
            services.AddAuthentication(opts =>
            {
                opts.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                opts.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(opts =>
            {
                opts.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context => Task.CompletedTask,
                    OnTokenValidated = context => Task.CompletedTask,
                    OnChallenge = context => throw new HalifaxUnauthorizedException("Request is unauthorized")
                };

                opts.RequireHttpsMetadata = true;
                opts.SaveToken = true;
                opts.TokenValidationParameters = builder.TokenValidationParameters;
            });
        }

        services.AddSwaggerGen(builder.OpenApi);
        services.AddCors();

        if (builder.useDefaultExceptionHandler)
        {
            services.AddExceptionHandler<HalifaxExceptionHandler>();
        }
    }

    /// <summary>
    /// Wires up the Halifax middleware pipeline: correlation IDs, request buffering, CORS, exception handling,
    /// routing, optional authentication/authorization, OpenAPI JSON, controller endpoints, and the Scalar
    /// API reference UI.
    /// </summary>
    /// <param name="app">The application builder to configure the middleware pipeline on.</param>
    /// <remarks>
    /// Call this after <see cref="AddHalifax"/>. Ordering matters: the <see cref="CorrelationIdMiddleware"/>
    /// runs first, request buffering is enabled so the body can be re-read by the exception handler, and the
    /// CORS and OpenAPI settings captured on the <see cref="HalifaxBuilder"/> are applied. Authentication and
    /// authorization middleware are only added when authentication was configured. The Swagger JSON is served
    /// at <c>openapi/{documentName}.json</c> and the Scalar reference UI is mapped alongside the controllers.
    /// </remarks>
    /// <exception cref="NullReferenceException">
    /// Thrown if the Halifax builder has not been initialized because <see cref="AddHalifax"/> was not called
    /// first.
    /// </exception>
    /// <example>
    /// <code>
    /// var app = builder.Build();
    /// app.UseHalifax();
    /// app.Run();
    /// </code>
    /// </example>
    public static void UseHalifax(this IApplicationBuilder app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();

        app.Use((context, next) =>
        {
            context.Request.EnableBuffering();
            return next();
        });

        app.UseCors(HalifaxBuilder.Instance!.Cors);

        app.UseExceptionHandler(configure => configure
            .Run(async handler => await Task.CompletedTask));

        app.UseRouting();

        if (HalifaxBuilder.Instance!.TokenValidationParameters != null)
        {
            app.UseAuthentication();
            app.UseAuthorization();
        }

        app.UseSwagger(c => c.RouteTemplate = "openapi/{documentName}.json");

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapScalarApiReference(options =>
            {
                options
                    .WithTitle(HalifaxBuilder.Instance!.Name)
                    .WithOpenApiRoutePattern("/openapi/{documentName}.json")
                    .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
            });
        });

        // This is necessary for IDEs to pick up server address and open browser automatically
        // var serverAddressesFeature = app.ServerFeatures.Get<IServerAddressesFeature>();
        // if (!serverAddressesFeature.Addresses.Any())
        // {
        //     serverAddressesFeature.Addresses.Add("http://localhost:5000");
        // }
        // serverAddressesFeature.Addresses.Each(address => L.Info($"Now listening on: {address}"));
    }
}

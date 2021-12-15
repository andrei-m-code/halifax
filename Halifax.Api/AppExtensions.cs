using Halifax.Api.App;
using Halifax.Api.Errors;
using Halifax.Core;
using Halifax.Core.Helpers;
using Halifax.Domain.Exceptions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Halifax.Api;

public static class AppExtensions
{
    public static void AddHalifax(this IServiceCollection services, Action<HalifaxBuilder> configure = null)
    {
        services.CleanupDefaultLogging();

        L.Info("Starting up Halifax");

        // Load .env configuration
        Env.Load();

        var builder = new HalifaxBuilder();
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
                    OnAuthenticationFailed = context =>
                    {
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        throw new HalifaxUnauthorizedException("Request is unauthorized");
                    }
                };

                opts.RequireHttpsMetadata = true;
                opts.SaveToken = true;
                opts.TokenValidationParameters = builder.TokenValidationParameters;
            });
        }

        services.AddSwaggerGen(builder.Swagger);
        services.AddCors();
        services.AddScoped(typeof(IExceptionHandler), builder.ExceptionHandlerType);
    }

    public static void UseHalifax(this IApplicationBuilder app)
    {
        app.UseCors(b => b.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin().AllowCredentials());
        app.UseExceptionHandler("/error");
        app.UseRouting();
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", HalifaxBuilder.Instance.Name));

        if (HalifaxBuilder.Instance.TokenValidationParameters != null)
        {
            app.UseAuthentication();
            app.UseAuthorization();
        }

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
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

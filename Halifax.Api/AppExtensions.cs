using Halifax.Api.App;
using Halifax.Api.Errors;
using Halifax.Core;
using Halifax.Core.Exceptions;
using Halifax.Core.Extensions;
using Halifax.Core.Helpers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Halifax.Api
{
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
            
            services
                .AddControllers()
                .AddJsonOptions(options => Json.ConfigureOptions(options.JsonSerializerOptions))
                .AddApplicationPart(typeof(AppExtensions).Assembly);

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
                        OnChallenge = context => context.AuthenticateFailure == null
                            ? Task.CompletedTask
                            : throw new HalifaxUnauthorizedException("Request is not authorized")
                    };

                    opts.RequireHttpsMetadata = true;
                    opts.SaveToken = true;
                    opts.TokenValidationParameters = builder.TokenValidationParameters;
                });
            }
            
            services.AddSwaggerGen(builder.Swagger);
            services.AddCors(opts => opts.AddPolicy("HalifaxCors", builder.Cors));
            services.AddScoped(typeof(IExceptionHandler), builder.ExceptionHandlerType);
        }

        public static void UseHalifax(this IApplicationBuilder app)
        {
            app.UseExceptionHandler("/error");
            app.UseRouting();
            app.UseCors("HalifaxCors");
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
                endpoints.MapGet("/", async context => { await context.Response.WriteAsync("Hello Halifax!"); });
            });

            // This is necessary for IDEs to pick up server address and open browser automatically
            var serverAddressesFeature = app.ServerFeatures.Get<IServerAddressesFeature>();
            if (!serverAddressesFeature.Addresses.Any())
            {
                serverAddressesFeature.Addresses.Add("http://localhost:5000");
            }
            serverAddressesFeature.Addresses.Each(address => L.Info($"Now listening on: {address}"));
        }
    }
}
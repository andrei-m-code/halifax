using Halifax.Api.App;
using Halifax.Core;
using Halifax.Core.Exceptions;
using Halifax.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;

namespace Halifax.Api
{
    public static class AppExtensions
    {
        public static void AddHalifax(this IServiceCollection services, Action<HalifaxBuilder> configure = null)
        {
            var builder = new HalifaxBuilder();
            configure?.Invoke(builder);
                        
            // Load .env configuration
            Env.Load();
            
            services
                .AddControllers()
                .AddApplicationPart(typeof(ApiResponse).Assembly);

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
        }
    }
}
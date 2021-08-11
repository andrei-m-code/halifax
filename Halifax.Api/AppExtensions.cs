using System;
using Halifax.Api.App;
using Halifax.Core;
using Halifax.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

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

            services.AddSwaggerGen(builder.Swagger);
            services.AddCors(opts => opts.AddPolicy("HalifaxCors", builder.Cors));
        }

        public static void UseHalifax(this IApplicationBuilder app)
        {
            app.UseExceptionHandler("/error");
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapGet("/", async context => { await context.Response.WriteAsync("Hello Halifax!"); });
            });

            app.UseCors("HalifaxCors");
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", HalifaxBuilder.Instance.Name));
        }
    }
}
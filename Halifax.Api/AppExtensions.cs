using Halifax.Api.Extensions;
using Halifax.Core;
using Halifax.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Halifax.Api
{
    public static class AppExtensions
    {
        public static void AddHalifax(this IServiceCollection services)
        {
            // Load .env configuration
            Env.Load();
            
            services
                .AddControllers()
                .AddApplicationPart(typeof(ApiResponse).Assembly);
            
            services.AddHalifaxSwagger();

            services.AddCors(opts => opts.AddPolicy("any", builder => builder
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowAnyOrigin()
            ));
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

            app.UseCors("any");
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", Utils.GetAppName()));
        }
    }
}
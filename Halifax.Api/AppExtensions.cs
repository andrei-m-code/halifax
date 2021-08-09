using Halifax.Api.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Halifax.Api
{
    public static class AppExtensions
    {
        public static void AddHalifax(this IServiceCollection services)
        {
            services
                .AddControllers()
                .AddApplicationPart(typeof(ApiResponse).Assembly);
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
        }
    }
}
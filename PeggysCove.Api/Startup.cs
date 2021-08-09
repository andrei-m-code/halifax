using Halifax.Api;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace PeggysCove.Api
{
    /// <summary>
    /// Peggy's Cove API startup
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// Configuring necessary dependencies to run the API
        /// </summary>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHalifax();
        }

        /// <summary>
        /// Registering middlewares 
        /// </summary>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseHalifax();
        }
    }
}
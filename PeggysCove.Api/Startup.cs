using System;
using System.Collections.Generic;
using System.Security.Claims;
using Halifax.Api;
using Halifax.Core;
using Halifax.Core.Helpers;
using Microsoft.AspNetCore.Builder;
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
            var secret = "Test JWT Token (at least 16 chars)";
            services.AddHalifax(builder => builder
                .SetName(Env.GetSection<AppSettings>().AppName)
                .ConfigureAuthentication(secret, false, false, false));

            var jwt = Jwt.Create(secret, new List<Claim>(), DateTime.UtcNow.AddYears(1));
            L.Info(jwt);
        }

        /// <summary>
        /// Registering middlewares 
        /// </summary>
        public void Configure(IApplicationBuilder app)
        {
            app.UseHalifax();
        }
    }
}
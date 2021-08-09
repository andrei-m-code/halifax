using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace Halifax.Api.Extensions
{
    public static class SwaggerExtensions
    {
        public static void AddHalifaxSwagger(this IServiceCollection services)
        {
            services.AddSwaggerGen(opts =>
            {
                opts.SwaggerDoc("v1", new OpenApiInfo { Title = Utils.GetAppName(), Version = "v1" });
                opts.AddSecurityDefinition("Bearer",
                    new OpenApiSecurityScheme
                    {
                        Description = "JWT Authorization header using the Bearer scheme.",
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer"
                    });

                opts.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference 
                            { 
                                Id = "Bearer", 
                                Type = ReferenceType.SecurityScheme 
                            }
                        },
                        new List<string>()
                    }
                });

                Directory
                    .GetFiles(AppContext.BaseDirectory)
                    .Where(file => file.EndsWith(".xml"))
                    .ToList()
                    .ForEach(file => opts.IncludeXmlComments(file));
            });
        }
    }
}
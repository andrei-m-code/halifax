using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Halifax.Api.App.Defaults
{
    internal static class SwaggerDefaults
    {
        internal static Action<SwaggerGenOptions> Value { get; } = opts =>
        {
            opts.SwaggerDoc("v1", new OpenApiInfo { Title = HalifaxBuilder.Instance.Name, Version = "v1" });

            if (HalifaxBuilder.Instance.TokenValidationParameters != null)
            {
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
            }

            Directory
                .GetFiles(AppContext.BaseDirectory)
                .Where(file => file.EndsWith(".xml"))
                .ToList()
                .ForEach(file => opts.IncludeXmlComments(file));
        };
    }
}
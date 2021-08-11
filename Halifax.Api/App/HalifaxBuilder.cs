using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace Halifax.Api.App
{
    public class HalifaxBuilder
    {
        internal static HalifaxBuilder Instance { get; set; }
        
        internal HalifaxBuilder()
        {
            if (Instance != null)
            {
                throw new InvalidOperationException("Halifax app can only be initialized once");
            }

            Instance = this;
        }
        
        internal string Name { get; private set; } = AppDomain.CurrentDomain.FriendlyName;

        internal Action<CorsPolicyBuilder> Cors { get; private set; } = cors => cors
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowAnyOrigin();

        internal Action<SwaggerGenOptions> Swagger { get; private set; } = opts =>
        {
            opts.SwaggerDoc("v1", new OpenApiInfo { Title = Instance.Name, Version = "v1" });
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
        };
        
        public HalifaxBuilder SetName(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            return this;
        }

        public HalifaxBuilder ConfigureCors(Action<CorsPolicyBuilder> corsPolicyBuilder)
        {
            Cors = corsPolicyBuilder ?? throw new ArgumentNullException(nameof(corsPolicyBuilder));
            return this;
        }

        public HalifaxBuilder ConfigureSwagger(Action<SwaggerGenOptions> swaggerBuilder)
        {
            Swagger = swaggerBuilder ?? throw new ArgumentNullException(nameof(swaggerBuilder));
            return this;
        }
    }
}
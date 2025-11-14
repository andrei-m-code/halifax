using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Halifax.Api.App.Defaults;

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

            opts.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Name = "Authorization",
                Description = "JWT Authorization header using the Bearer scheme."
            });
        }

        Directory
            .GetFiles(AppContext.BaseDirectory)
            .Where(file => file.EndsWith(".xml"))
            .ToList()
            .ForEach(file => opts.IncludeXmlComments(file));
    };
}

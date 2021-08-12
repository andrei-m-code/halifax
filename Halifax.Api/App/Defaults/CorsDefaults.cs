using Microsoft.AspNetCore.Cors.Infrastructure;
using System;

namespace Halifax.Api.App.Defaults
{
    internal static class CorsDefaults
    {
        internal static Action<CorsPolicyBuilder> Value { get; } = cors => cors
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowAnyOrigin();
    }
}
using System.Security.Claims;
using Halifax.Api;
using Halifax.Core;
using Halifax.Core.Helpers;
using PeggysCove.Api;

var secret = "Test JWT Token (at least 16 chars)";
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHalifax(builder => builder
    .SetName(Env.GetSection<AppSettings>().AppName)
    .ConfigureAuthentication(secret, false, false, false));

var jwt = Jwt.Create(secret, new List<Claim>(), DateTime.UtcNow.AddYears(1));
L.Info(jwt);

var app = builder.Build();
app.UseHalifax();

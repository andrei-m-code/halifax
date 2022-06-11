using Halifax.Api;
using PeggysCove.Api;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHalifax(halifax => halifax
    .AddSettings<AppSettings>()
    .ConfigureAuthentication(TokenHelper.JwtSecret, false, false, false));

var jwt = TokenHelper.CreateToken();
L.Info(jwt);

var app = builder.Build();
app.UseHalifax();
app.Run("http://*:5000");

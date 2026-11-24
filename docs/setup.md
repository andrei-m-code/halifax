# Service setup

`AddHalifax` and `UseHalifax` wire up an ASP.NET Core API service. Everything below is optional: the defaults work without any configuration.

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHalifax(h => h
    .SetName("Orders API")
    .AddSettings<AppSettings>()
    .ConfigureAuthentication(jwtSecret));

var app = builder.Build();
app.UseHalifax();
app.Run();
```

## What you get

| Piece | Default |
|---|---|
| Controllers | Your controllers plus Halifax's own (the version endpoint) |
| Exception handling | Halifax exceptions mapped to status codes, see [Responses and errors](errors.md) |
| OpenAPI | Swashbuckle document at `/openapi/v1.json`, with a Bearer scheme and your XML comments |
| API reference UI | Scalar at `/scalar` |
| CORS | Any origin, header and method |
| Correlation IDs | `X-Correlation-Id` read or generated, see [HTTP client](http-client.md#correlation-ids) |
| JSON | camelCase, case-insensitive reads, enums as strings, UTC dates |
| Configuration | `.env` loaded, see [Configuration](configuration.md) |
| Logging | Console logger `L`; ASP.NET Core's own logs quiet, see [Logging](logging.md) |
| Version endpoint | `GET /halifax/version` |

Halifax can be set up once per process: a second `AddHalifax` throws `InvalidOperationException`. Tests that host the app should share one instance.

## Builder methods

| Method | What it does |
|---|---|
| `SetName(name)` | Title of the OpenAPI document and the Scalar page |
| `AddSettings<T>()`, `AddSettings<T>(out T settings)` | Binds a settings type from environment variables and registers it as a singleton |
| `ConfigureAuthentication(secret, ...)`, `ConfigureAuthentication(TokenValidationParameters)` | JWT bearer authentication, see [Authentication](authentication.md) |
| `ConfigureCors(cors => ...)` | Replaces the CORS policy |
| `ConfigureOpenApi(options => ...)` | Replaces the Swashbuckle setup |
| `ConfigureJson(options => ...)` | Replaces the JSON options |
| `ConfigureMvc(mvc => ...)` | Extra MVC setup, e.g. more application parts or formatters |
| `ConfigureExceptionHandler(false)` | Turns off the Halifax exception handler |
| `ConfigureErrorTrace(false)` | Leaves the stack trace out of error responses |
| `ConfigureFrameworkLogging(level)` | Shows ASP.NET Core and runtime logs from `level` up |

`ConfigureCors`, `ConfigureOpenApi` and `ConfigureJson` replace the Halifax defaults instead of adding to them:

- a custom OpenAPI setup loses the default v1 document, the Bearer scheme and the XML comments;
- custom JSON options also apply to `Json` and `HalifaxHttpClient` across the app.

## Version endpoint

`GET /halifax/version` lists the loaded Halifax assemblies:

```json
{
  "data": [
    { "name": "Halifax.Api", "version": "6.2.0.0" },
    { "name": "Halifax.Core", "version": "6.2.0.0" }
  ],
  "success": true,
  "error": null
}
```

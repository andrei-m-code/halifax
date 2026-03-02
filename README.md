# Halifax Service Foundation

Simplistic libraries for complex projects. Halifax eliminates boilerplate in .NET API services — standardized responses, JWT auth, configuration, logging, and more — so you can focus on business logic.

[![CI](https://github.com/andrei-m-code/halifax/actions/workflows/ci.yml/badge.svg)](https://github.com/andrei-m-code/halifax/actions/workflows/ci.yml)

| Package | NuGet |
|---|---|
| Halifax.Api | [![NuGet](https://img.shields.io/nuget/v/Halifax.Api.svg)](https://www.nuget.org/packages/Halifax.Api/) |
| Halifax.Core | [![NuGet](https://img.shields.io/nuget/v/Halifax.Core.svg)](https://www.nuget.org/packages/Halifax.Core/) |
| Halifax.Domain | [![NuGet](https://img.shields.io/nuget/v/Halifax.Domain.svg)](https://www.nuget.org/packages/Halifax.Domain/) |
| Halifax.Http | [![NuGet](https://img.shields.io/nuget/v/Halifax.Http.svg)](https://www.nuget.org/packages/Halifax.Http/) |
| Halifax.Excel | [![NuGet](https://img.shields.io/nuget/v/Halifax.Excel.svg)](https://www.nuget.org/packages/Halifax.Excel/) |

## Features

- **Standardized API responses** — consistent `ApiResponse` wrapper for all endpoints
- **Exception handling** — throw typed exceptions, get proper HTTP status codes automatically
- **JWT authentication** — configure auth in one line, create and validate tokens easily
- **Environment configuration** — load `.env` files, map to strongly-typed classes/records
- **Input validation** — fluent `Guard` helpers for common checks
- **HTTP client base class** — typed `HttpClient` with automatic error mapping
- **OpenAPI + Scalar UI** — Swagger docs with interactive API explorer out of the box
- **Logging** — Serilog-based structured logging
- **CORS** — configurable cross-origin policy
- **Excel/CSV** — import and export with column mapping
- **Cryptography** — AES-256 encrypt/decrypt helpers
- **Short IDs** — thread-safe random ID generation

## Quick Start

```
dotnet add package Halifax.Api
```

```csharp
using Halifax.Api;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHalifax();

var app = builder.Build();
app.UseHalifax();
app.Run("https://*:5000");
```

This gives you controller routing, exception handling, Swagger, Scalar UI, CORS, and structured logging. Explore the [Peggy's Cove](https://github.com/andrei-m-code/halifax/blob/main/PeggysCove.Api/Program.cs) sample project for a full working example.

## API Responses

All endpoints return a consistent format using `ApiResponse`:

```csharp
// Return data
return ApiResponse.With(user);

// Return empty success
return ApiResponse.Empty;
```

Response format:

```json
{
  "data": { ... },
  "success": true,
  "error": null
}
```

On error:

```json
{
  "data": null,
  "success": false,
  "error": {
    "type": "HalifaxNotFoundException",
    "message": "User not found"
  }
}
```

### Pagination

```csharp
[HttpGet]
public ApiResponse<Paging<UserDto>> GetUsers([FromQuery] PagingQuery query)
{
    var items = db.Users.Skip(query.Skip).Take(query.Take).ToList();
    var total = db.Users.Count();
    return ApiResponse.With(new Paging<UserDto>(items, query.Skip, query.Take, total));
}
```

`PagingQuery` binds `Skip`, `Take`, `OrderBy`, and `OrderDirection` from query string parameters.

## Exceptions

Throw typed exceptions anywhere in your code — the middleware handles the HTTP response:

| Exception | HTTP Status |
|---|---|
| `HalifaxException` | 400 Bad Request |
| `HalifaxNotFoundException` | 404 Not Found |
| `HalifaxUnauthorizedException` | 401 Unauthorized |

```csharp
var user = await db.Users.FindAsync(id);
if (user == null)
    throw new HalifaxNotFoundException("User not found");
```

For advanced scenarios, override `DefaultExceptionHandler` or register your own `IExceptionHandler`.

## Configuration

Halifax loads environment variables from `.env` files automatically. Define a class or record matching your variable names:

```dotenv
AppSettings__ConnectionString=localhost
AppSettings__HttpTimeout=120
```

```csharp
record AppSettings(string ConnectionString, int HttpTimeout);
```

Register during startup:

```csharp
builder.Services.AddHalifax(h => h.AddSettings<AppSettings>());
```

Settings are registered as singletons — inject them into controllers and services, or access them directly:

```csharp
var settings = Env.GetSection<AppSettings>();
```

Supported types: `string`, primitives, `DateTime`, `TimeSpan`, `Guid`, and their nullable variants.

## JWT Authentication

Enable authentication with one call:

```csharp
builder.Services.AddHalifax(h => h
    .ConfigureAuthentication("your_jwt_secret_min_16_chars",
        validateAudience: false,
        validateIssuer: false,
        requireExpirationTime: false));
```

All non-`[AllowAnonymous]` endpoints now require `Authorization: Bearer {token}`.

### Creating Tokens

```csharp
var claims = new List<Claim>
{
    new("sub", user.Id.ToString()),
    new("email", user.Email),
    new("role", user.Role)
};

var token = Jwt.Create("your_jwt_secret", claims, DateTime.UtcNow.AddDays(30));
```

### Reading Tokens

```csharp
var principal = Jwt.Read("your_jwt_secret", token);
```

### Claims-Based Authorization

Create custom authorization filters by extending `ClaimsAuthorizeFilterAttribute`:

```csharp
class AdminOnly : ClaimsAuthorizeFilterAttribute
{
    protected override bool IsAuthorized(ActionExecutingContext context, List<Claim> claims)
    {
        claims.ClaimExpected("role", "admin");
        return true;
    }
}

[HttpGet("admin")]
[AdminOnly]
public ApiResponse GetAdminData() => ApiResponse.With("secret");
```

Available claim extensions: `ClaimExpected`, `ClaimNotNullOrWhiteSpace`, `ClaimIsEmail`, `ClaimIsInt`, `ClaimIsDouble`, `ClaimIsEnum<T>`, `ClaimIsGuid`, `ClaimIsBoolean`.

## Validation

`Guard` provides fluent validation that throws `HalifaxException` (400) on failure:

```csharp
Guard.NotNullOrWhiteSpace(request.Name, nameof(request.Name));
Guard.Email(request.Email);
Guard.Length(request.Password, nameof(request.Password), lower: 8, upper: 64);
Guard.Range(request.Age, nameof(request.Age), from: 18, to: 120);
Guard.Url(request.Website, nameof(request.Website));
Guard.Ensure(request.AcceptedTerms, "Terms must be accepted");
Guard.NotNull(request.Address, nameof(request.Address));
Guard.NotEmptyList(request.Tags, nameof(request.Tags));
Guard.Color(request.Theme, nameof(request.Theme));
```

## HTTP Client

Create typed HTTP clients for service-to-service communication by extending `HalifaxHttpClient`:

```csharp
public class PaymentClient(HttpClient http) : HalifaxHttpClient(http)
{
    public async Task<PaymentDto> GetAsync(string id)
    {
        var msg = CreateMessage(HttpMethod.Get, $"/api/payments/{id}");
        return await SendAsync<PaymentDto>(msg);
    }

    public async Task<HttpStatusCode> CreateAsync(CreatePaymentRequest request)
    {
        var msg = CreateMessage(HttpMethod.Post, "/api/payments", request);
        return await SendAsync(msg);
    }
}
```

Register with optional defaults:

```csharp
services.AddHalifaxHttpClient<PaymentClient>(
    defaultBaseUrl: "https://payments.api.com",
    defaultBearerToken: token);
```

Error responses (400, 401, 404) from downstream services are automatically mapped to the corresponding Halifax exceptions.

## Excel & CSV

```
dotnet add package Halifax.Excel
```

```csharp
var converter = new ExcelConverter<Person>();
converter.AddMapping("Full Name", p => p.Name);
converter.AddMapping("Age", p => p.Age);

// Write
using var stream = new MemoryStream();
await converter.WriteExcelAsync(stream, people, "Sheet1");
await converter.WriteCsvAsync(stream, people);

// Read (auto-detects format from content type)
var records = await converter.ReadAsync(fileStream, contentType);
```

## Utilities

### Cryptography

AES-256 encryption:

```csharp
var encrypted = Crypto.Encrypt("secret", "sensitive data");
var decrypted = Crypto.Decrypt("secret", encrypted);
Crypto.TryDecrypt("secret", encrypted, out var result);
```

### Short IDs

Thread-safe random ID generation:

```csharp
var id = ShortId.Create();              // e.g. "kX9mBnQ"
var id = ShortId.Create(length: 12);    // longer ID
var id = ShortId.Create(useNumbers: false); // letters only
```

### JSON

Pre-configured serialization (camelCase, case-insensitive, enums as strings, UTC dates):

```csharp
var json = Json.Serialize(obj);
var obj = Json.Deserialize<MyType>(json);
```

### Logging

Global structured logging via Serilog:

```csharp
L.Info("User created", userId);
L.Warning("Timeout exceeded");
L.Error(exception, "Failed to process");
```

## Full Configuration Example

```csharp
builder.Services.AddHalifax(h => h
    .SetName("My Service")
    .AddSettings<AppSettings>()
    .AddSettings<DatabaseSettings>()
    .ConfigureAuthentication(jwtSecret, false, false, false)
    .ConfigureCors(cors => cors
        .AllowAnyHeader()
        .AllowAnyMethod()
        .WithOrigins("https://myapp.com"))
    .ConfigureOpenApi(swagger => { /* customize Swashbuckle */ })
    .ConfigureJson(opts => { /* customize System.Text.Json */ }));
```

## Package Overview

| Package | Purpose | Dependencies |
|---|---|---|
| **Halifax.Domain** | Response models, exceptions, pagination | None |
| **Halifax.Core** | JWT, config, validation, crypto, logging, JSON | Halifax.Domain, Serilog, System.IdentityModel.Tokens.Jwt |
| **Halifax.Api** | ASP.NET Core integration, middleware, Swagger | Halifax.Core, Swashbuckle, Scalar, JwtBearer |
| **Halifax.Http** | Typed HttpClient base class | Halifax.Core |
| **Halifax.Excel** | Excel/CSV import and export | CsvHelper, ExcelMapper, NPOI |

All packages target **.NET 10**.

## License

MIT - see [LICENSE](LICENSE) for details.

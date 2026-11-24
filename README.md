# Halifax Service Foundation

Simple libraries for complex projects. Halifax removes the boilerplate from .NET API services: standardized responses, exception mapping, JWT auth, configuration, logging and more, so you can focus on business logic.

[![CI](https://github.com/andrei-m-code/halifax/actions/workflows/ci.yml/badge.svg)](https://github.com/andrei-m-code/halifax/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Halifax.Api.svg?label=Halifax.Api)](https://www.nuget.org/packages/Halifax.Api/)

| Package | NuGet | Description |
|---|---|---|
| Halifax.Api | [![NuGet](https://img.shields.io/nuget/v/Halifax.Api.svg)](https://www.nuget.org/packages/Halifax.Api/) | ASP.NET Core integration: middleware, OpenAPI, Scalar UI |
| Halifax.Core | [![NuGet](https://img.shields.io/nuget/v/Halifax.Core.svg)](https://www.nuget.org/packages/Halifax.Core/) | JWT, configuration, validation, crypto, logging, JSON |
| Halifax.Domain | [![NuGet](https://img.shields.io/nuget/v/Halifax.Domain.svg)](https://www.nuget.org/packages/Halifax.Domain/) | Response models, exceptions, pagination |
| Halifax.Http | [![NuGet](https://img.shields.io/nuget/v/Halifax.Http.svg)](https://www.nuget.org/packages/Halifax.Http/) | Typed HttpClient with error mapping and resilience |
| Halifax.Excel | [![NuGet](https://img.shields.io/nuget/v/Halifax.Excel.svg)](https://www.nuget.org/packages/Halifax.Excel/) | Excel and CSV import and export |

## Features

- **Standardized API responses:** one `ApiResponse<T>` envelope for every endpoint
- **Exception mapping:** throw a typed exception, get the matching HTTP status
- **JWT authentication:** configure it in one call, create and read tokens
- **Environment configuration:** load `.env` files into strongly-typed records
- **Validation:** `Guard` helpers that throw a 400 on bad input
- **Correlation IDs:** `X-Correlation-Id` on every request and response
- **HTTP client base class:** typed clients with error mapping and resilience policies
- **OpenAPI and Scalar UI** out of the box
- **Logging:** a static Serilog console logger
- **CORS:** open by default, configurable
- **Excel and CSV:** import and export with column mapping
- **Utilities:** AES encryption, short IDs, JSON, date helpers
- **Nullable reference types and XML docs** in every package

## Compatibility

Each Halifax major version targets one .NET major version. Pick the line that matches your app's target framework, and keep all `Halifax.*` packages in one app on the same major version.

| Halifax.Api, .Core, .Domain, .Http | Halifax.Excel | .NET | Target framework |
|---|---|---|---|
| **6.x** | **6.x** | **.NET 11** | `net11.0` |
| 5.x | 0.2.x to 0.4.x | .NET 10 | `net10.0` |
| 4.x | 0.1.x | .NET 9 | `net9.0` |
| 3.x | 0.0.6 to 0.0.11 | .NET 8 | `net8.0` |
| 2.x | 0.0.1 to 0.0.5 | .NET 7 | `net7.0` |
| 1.x | not available | .NET 6 | `net6.0` |

Starting with 6.0.0, Halifax.Excel is stable and shares the major version with the other packages. Older lines stay on NuGet but get no further updates; new features and fixes go to the latest line only.

## Quick Start

```bash
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

This gives you controllers, exception mapping, an OpenAPI document (Swashbuckle) at `/openapi/v1.json`, the Scalar UI at `/scalar`, CORS that allows any origin, correlation IDs, the [version endpoint](#version-endpoint) and console logging. See the [Peggy's Cove](https://github.com/andrei-m-code/halifax/blob/main/PeggysCove.Api/Program.cs) sample for a full working app.

Halifax can be set up once per process: a second `AddHalifax` throws `InvalidOperationException`.

## API Responses

Return `ApiResponse` from every endpoint:

```csharp
return ApiResponse.With(user);   // data
return ApiResponse.Empty;        // success, no data
```

Success:

```json
{
  "data": { "id": 1, "name": "Alice" },
  "success": true,
  "error": null
}
```

Error (written by the exception handler):

```json
{
  "success": false,
  "error": {
    "type": "HalifaxNotFoundException",
    "message": "User not found",
    "trace": "   at UsersController.Get(...)"
  }
}
```

`trace` is the exception's stack trace and is included for every error, 500s too.

### Pagination

```csharp
[HttpGet]
public async Task<ApiResponse<Paging<UserDto>>> GetUsers([FromQuery] PagingQuery query)
{
    var items = await db.Users
        .OrderBy(u => u.Name)
        .Skip(query.Skip).Take(query.Take)
        .Select(u => new UserDto(u.Id, u.Name))
        .ToListAsync();
    var total = await db.Users.CountAsync();

    return ApiResponse.With(new Paging<UserDto>(items, query.Skip, query.Take, total));
}
```

`PagingQuery` binds `Skip` (default 0), `Take` (default 10), `OrderBy` and `OrderDirection` (`Asc`/`Desc`) from the query string. `PagingMore<T>` carries a `HasMore` flag instead of a total, and both have `Map<TDest>()` to convert their items.

## Exceptions

Throw a typed exception anywhere; the exception handler writes the HTTP response:

| Exception | HTTP status |
|---|---|
| `HalifaxException` | 400 Bad Request |
| `HalifaxUnauthorizedException` | 401 Unauthorized |
| `HalifaxNotFoundException` | 404 Not Found |
| any other exception | 500 Internal Server Error |

```csharp
var user = await db.Users.FindAsync(id)
    ?? throw new HalifaxNotFoundException("User not found");
```

To handle exceptions yourself, turn off the built-in handler and register your own `IExceptionHandler`. To keep Halifax's mapping and change only how errors are logged, derive from `HalifaxExceptionHandler`, override `LogErrorRequestAsync` and register that instead.

```csharp
builder.Services.AddHalifax(h => h.ConfigureExceptionHandler(false));
builder.Services.AddExceptionHandler<MyExceptionHandler>();
```

## Configuration

`AddHalifax` loads the nearest `.env` file, searching from the current directory upward, into the process environment. A missing file is ignored, and its values override variables that are already set.

```dotenv
AppSettings__ConnectionString=localhost
AppSettings__HttpTimeout=120
```

```csharp
record AppSettings(string ConnectionString, int HttpTimeout);

builder.Services.AddHalifax(h => h.AddSettings<AppSettings>());
```

`AddSettings<T>()` binds `T` from variables named `{TypeName}__{Member}` and registers it as a singleton; `AddSettings<T>(out var settings)` also hands it back during startup. Outside dependency injection, use `Env.GetSection<AppSettings>()` (cached per type).

Supported types: `string`, `byte`, `short`, `int`, `long`, `float`, `double`, `decimal`, `Guid`, `DateTime`, `TimeSpan` and their nullable versions, parsed with the current culture. Prefer records: settable properties skip `decimal` and nullable types. `bool` is not supported.

## JWT Authentication

```csharp
builder.Services.AddHalifax(h => h
    .ConfigureAuthentication(jwtSecret,
        validateAudience: false,
        validateIssuer: false,
        requireExpirationTime: false));
```

Endpoints marked `[Authorize]` (on the action or the controller) then require `Authorization: Bearer {token}`. Use an ASCII secret of at least 32 characters, since HS256 needs a 256-bit key. For full control, pass your own `TokenValidationParameters` to `ConfigureAuthentication`.

### Creating and Reading Tokens

```csharp
var claims = new List<Claim>
{
    new("sub", user.Id.ToString()),
    new("email", user.Email),
    new("role", user.Role)
};

string token = Jwt.Create(jwtSecret, claims, DateTime.UtcNow.AddDays(30));

List<Claim> read = Jwt.Read(jwtSecret, token);
```

`Jwt.Read` throws `HalifaxUnauthorizedException` for an invalid token, or returns an empty list with `throwUnauthorized: false`. It does not check expiry unless you pass `validateLifetime: true`.

### Claims-Based Authorization

Derive from `ClaimsAuthorizeFilterAttribute` for custom checks. A failed check returns 401.

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
public ApiResponse<string> GetAdminData() => ApiResponse.With("secret");
```

Claim extensions: `ClaimExpected`, `ClaimNotNullOrWhiteSpace`, `ClaimIsEmail`, `ClaimIsInt`, `ClaimIsDouble`, `ClaimIsEnum<T>`, `ClaimIsGuid`, `ClaimIsBoolean`, `ClaimIs<T>` (any `IParsable<T>`) and `ClaimValidate` (your own predicate). Each has an overload that returns the parsed value through `out`.

## Correlation IDs

`UseHalifax` adds `CorrelationIdMiddleware`, which:

- reads the incoming `X-Correlation-Id` header, or generates a new GUID when it is missing
- sets `HttpContext.TraceIdentifier` to it
- adds `X-Correlation-Id` to the response

To forward the ID on outgoing calls, attach `CorrelationIdDelegatingHandler` to your Halifax HTTP client:

```csharp
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<CorrelationIdDelegatingHandler>();
builder.Services
    .AddHalifaxHttpClientBuilder<PaymentClient>("https://payments.api.com", null, null)
    .AddHttpMessageHandler<CorrelationIdDelegatingHandler>();
```

## Validation

`Guard` methods throw `HalifaxException` (400) when a check fails:

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

Also available: `Guard.StringWithEmails` for a list of addresses in one string.

## HTTP Client

Derive from `HalifaxHttpClient` for typed service-to-service clients:

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

`SendAsync<T>` deserializes the response and maps errors: a 400, 401 or 404 whose body carries an `error.message` becomes `HalifaxException`, `HalifaxUnauthorizedException` or `HalifaxNotFoundException`; any other failure throws `Exception`. Override `HandleUnsuccessfulResponseAsync` to change that. `SendAsync` without a type only returns the status code and never throws on an error status.

Register the client with a base URL (required, absolute) and an optional bearer token:

```csharp
builder.Services.AddHalifaxHttpClient<PaymentClient>(
    defaultBaseUrl: "https://payments.api.com",
    defaultBearerToken: token);
```

Other overloads take an `Action<HttpClient>` or `Action<IServiceProvider, HttpClient>` to configure the client.

### Resilience Policies

```csharp
// Standard resilience: retry, circuit breaker and timeout
builder.Services.AddHalifaxHttpClientWithResilience<PaymentClient>(
    defaultBaseUrl: "https://payments.api.com");

// Or take the IHttpClientBuilder and configure it yourself
builder.Services.AddHalifaxHttpClientBuilder<PaymentClient>(
        defaultBaseUrl: "https://payments.api.com",
        defaultBearerToken: null,
        configure: null)
    .AddStandardResilienceHandler(options => options.Retry.MaxRetryAttempts = 3);
```

## Excel & CSV

```bash
dotnet add package Halifax.Excel
```

```csharp
var converter = new ExcelConverter<Person>();
converter.AddMapping("Full Name", p => p.Name);
converter.Exclude(p => p.InternalId);   // leave a property out of the file

// Read .xlsx, .xls or CSV (format picked from the content type)
var records = await converter.ReadAsync(file.OpenReadStream(), file.ContentType);

// Write: the stream stays open and is rewound, ready to return or upload
var stream = new MemoryStream();
await converter.WriteExcelAsync(stream, people, "People");
return File(stream, ExcelContentType.Xlsx, "People.xlsx");   // FileStreamResult disposes it
```

`ReadExcel(stream)` and `ReadExcel(stream, sheetName)` read Excel directly; `ReadCsvAsync` and `WriteCsvAsync` do CSV, using the converter's `CultureInfo` for values and the separator.

Several sheets with different record types go into one workbook with `ExcelWorkbookWriter`; each sheet takes its own converter for mappings and exclusions:

```csharp
using var writer = new ExcelWorkbookWriter();
writer.AddSheet("People", people, converter)
      .AddSheet("Orders", orders);
writer.Write(stream);
```

Reading Excel matches headers case-insensitively, skips blank rows and fills constructor-only records. A cell that cannot be converted throws `ExcelConversionException` with the row and column, ready to show to whoever uploaded the file. CSV reads match mapped headers exactly and skip values that do not convert. Dates are written as real date cells (`DateTimeFormat`, `DateFormat`).

Halifax.Excel neither ships nor loads SkiaSharp. If your own code uses NPOI directly, note that NPOI 2.8 needs SkiaSharp for `DataFormatter` and for `ToString()` on .xls cells but does not ship it, so those calls fail at runtime. `ExcelCell.GetText(cell)` is a SkiaSharp-free replacement for `cell?.ToString()`.

## Utilities

### Cryptography

```csharp
var encrypted = Crypto.Encrypt(secret, "sensitive data");
var decrypted = Crypto.Decrypt(secret, encrypted);
Crypto.TryDecrypt(secret, encrypted, out var result);
```

AES-256 (CBC) with the key and IV derived from the secret, so the same text always encrypts to the same value and there is no tamper check. Good for obscuring values at rest; for anything sensitive, use ASP.NET Core Data Protection or AES-GCM.

### Short IDs

```csharp
var id = ShortId.Create();                         // 7 characters, letters and digits
var longer = ShortId.Create(length: 12);           // the minimum length is 7
var letters = ShortId.Create(useNumbers: false);   // letters only
```

Short IDs are random, not guaranteed unique; use a `Guid` when uniqueness matters.

### JSON

`Json` serializes with camelCase names, case-insensitive reads, enums as strings and UTC dates:

```csharp
var json = Json.Serialize(obj);
var obj = Json.Deserialize<MyType>(json);
Json.TryDeserialize<MyType>(json, out var result);
```

### Date Helpers

```csharp
DateTimeHelper.ValidateRange(from, to);    // throws if from > to
DateTimeHelper.IsIn(from, to, pointInTime);

DateTime? date = DateTime.UtcNow;
date.ToIsoFormat();                        // "2026-11-24T10:30:00Z"
```

`ToIsoFormat` works on `DateTime?` and formats the value as it is, without converting it to UTC.

### Logging

`L` is a static Serilog logger that writes to the console:

```csharp
L.Info("User {UserId} created", userId);
L.Warning("Timeout after {Seconds} s", seconds);
L.Error(exception, "Failed to process order {OrderId}", orderId);
```

`AddHalifax` sets the `Microsoft` and `System` log categories to `Critical`, so ASP.NET Core's own logs stay quiet.

## Version Endpoint

`GET /halifax/version` returns the loaded Halifax assembly versions:

```json
{
  "data": [
    { "name": "Halifax.Api", "version": "6.0.0.0" },
    { "name": "Halifax.Core", "version": "6.0.0.0" }
  ],
  "success": true,
  "error": null
}
```

## Full Configuration Example

```csharp
builder.Services.AddHalifax(h => h
    .SetName("My Service")
    .AddSettings<AppSettings>()
    .AddSettings<DatabaseSettings>()
    .ConfigureAuthentication(jwtSecret)
    .ConfigureCors(cors => cors
        .AllowAnyHeader()
        .AllowAnyMethod()
        .WithOrigins("https://myapp.com"))
    .ConfigureOpenApi(swagger => { /* Swashbuckle SwaggerGenOptions */ })
    .ConfigureJson(options => { /* System.Text.Json options */ })
    .ConfigureMvc(mvc => { /* IMvcBuilder */ }));
```

`ConfigureCors`, `ConfigureOpenApi` and `ConfigureJson` replace the Halifax defaults instead of adding to them: a custom OpenAPI setup loses the default v1 document, the Bearer scheme and the XML comments, and custom JSON options also apply to `Json` and `HalifaxHttpClient` across the app.

## Package Overview

| Package | Dependencies |
|---|---|
| **Halifax.Domain** | none |
| **Halifax.Core** | Halifax.Domain, Serilog, Serilog.Sinks.Console, Destructurama.Attributed, System.IdentityModel.Tokens.Jwt, Microsoft.Extensions.Logging |
| **Halifax.Api** | Halifax.Core, Swashbuckle.AspNetCore, Scalar.AspNetCore, Microsoft.AspNetCore.Authentication.JwtBearer |
| **Halifax.Http** | Halifax.Core, Microsoft.Extensions.Http.Resilience |
| **Halifax.Excel** | CsvHelper, NPOI |

All packages target **.NET 11** with nullable reference types enabled. For older .NET versions see [Compatibility](#compatibility).

## MIT License

Copyright (c) 2020-2026 Andrei M

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

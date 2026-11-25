# Halifax Service Foundation

Simple libraries for complex projects. Halifax removes the boilerplate from .NET API services: standardized responses, exception mapping, JWT auth, configuration, logging and more, so you can focus on business logic.

[![CI](https://github.com/andrei-m-code/halifax/actions/workflows/ci.yml/badge.svg)](https://github.com/andrei-m-code/halifax/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Halifax.Api.svg?label=Halifax.Api)](https://www.nuget.org/packages/Halifax.Api/)

| Package | NuGet | Description |
|---|---|---|
| Halifax.Api | [![NuGet](https://img.shields.io/nuget/v/Halifax.Api.svg)](https://www.nuget.org/packages/Halifax.Api/) | ASP.NET Core integration: middleware, OpenAPI, Scalar UI |
| Halifax.Core | [![NuGet](https://img.shields.io/nuget/v/Halifax.Core.svg)](https://www.nuget.org/packages/Halifax.Core/) | JWT, configuration, validation, encryption, logging, JSON |
| Halifax.Domain | [![NuGet](https://img.shields.io/nuget/v/Halifax.Domain.svg)](https://www.nuget.org/packages/Halifax.Domain/) | Response models, exceptions, pagination |
| Halifax.Http | [![NuGet](https://img.shields.io/nuget/v/Halifax.Http.svg)](https://www.nuget.org/packages/Halifax.Http/) | Typed HttpClient with error mapping and resilience |
| Halifax.Excel | [![NuGet](https://img.shields.io/nuget/v/Halifax.Excel.svg)](https://www.nuget.org/packages/Halifax.Excel/) | Excel and CSV import and export |

## Documentation

This page is the overview. Each area has its own page with the details:

| Guide | Covers |
|---|---|
| [Service setup](https://github.com/andrei-m-code/halifax/blob/main/docs/setup.md) | `AddHalifax`/`UseHalifax`, every builder method, OpenAPI, Scalar, CORS, version endpoint |
| [Responses and errors](https://github.com/andrei-m-code/halifax/blob/main/docs/errors.md) | `ApiResponse`, exception mapping, stack traces in errors, custom handlers, pagination |
| [Configuration](https://github.com/andrei-m-code/halifax/blob/main/docs/configuration.md) | `.env` files, settings types, supported types, parsing rules |
| [Authentication](https://github.com/andrei-m-code/halifax/blob/main/docs/authentication.md) | JWT setup, secrets, creating and reading tokens, claims checks |
| [HTTP client](https://github.com/andrei-m-code/halifax/blob/main/docs/http-client.md) | Typed clients, error mapping, resilience, correlation IDs |
| [Logging](https://github.com/andrei-m-code/halifax/blob/main/docs/logging.md) | The `L` logger, ASP.NET Core's own logs |
| [Excel and CSV](https://github.com/andrei-m-code/halifax/blob/main/docs/excel.md) | Reading and writing, multi-sheet workbooks, SkiaSharp-free reading |
| [Utilities](https://github.com/andrei-m-code/halifax/blob/main/docs/utilities.md) | Encryption formats, short IDs, JSON, dates, extensions |

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

Minor and patch releases keep existing behavior as the default: anything that would change what a running service returns, stores or logs is opt-in.

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

This gives you controllers, exception mapping, an OpenAPI document at `/openapi/v1.json`, the Scalar UI at `/scalar`, CORS that allows any origin, correlation IDs, a version endpoint and console logging. See the [Peggy's Cove](https://github.com/andrei-m-code/halifax/blob/main/PeggysCove.Api/Program.cs) sample for a full working app, and [Service setup](https://github.com/andrei-m-code/halifax/blob/main/docs/setup.md) for every option.

## Responses and Errors

```csharp
[HttpGet("{id}")]
public async Task<ApiResponse<UserDto>> Get(int id)
{
    var user = await db.Users.FindAsync(id)
        ?? throw new HalifaxNotFoundException("User not found");   // 404

    return ApiResponse.With(new UserDto(user.Id, user.Name));
}
```

Success is `{ "data": ..., "success": true, "error": null }`. `HalifaxException` maps to 400, `HalifaxUnauthorizedException` to 401, `HalifaxNotFoundException` to 404 and anything else to 500, with `{ "success": false, "error": { "type", "message", "trace" } }`. `trace` is the stack trace; turn it off for public APIs with `ConfigureErrorTrace(false)`. [More](https://github.com/andrei-m-code/halifax/blob/main/docs/errors.md)

## Configuration

```dotenv
AppSettings__ConnectionString=localhost
AppSettings__HttpTimeout=120
```

```csharp
record AppSettings(string ConnectionString, int HttpTimeout);

builder.Services.AddHalifax(h => h.AddSettings<AppSettings>());
```

`AddHalifax` loads the nearest `.env` file; `AddSettings<T>()` binds `T` from `{TypeName}__{Member}` variables and registers it as a singleton. Strings, `bool`, numbers, `decimal`, `Guid`, dates, times, enums and their nullable versions are supported, parsed with the invariant culture. [More](https://github.com/andrei-m-code/halifax/blob/main/docs/configuration.md)

## Authentication

```csharp
builder.Services.AddHalifax(h => h.ConfigureAuthentication(jwtSecret));

string token = Jwt.Create(jwtSecret, claims, DateTime.UtcNow.AddDays(30));
List<Claim> read = Jwt.Read(jwtSecret, token);
```

Endpoints marked `[Authorize]` then require `Authorization: Bearer {token}`. Use a secret of at least 32 characters. For checks on claims, derive from `ClaimsAuthorizeFilterAttribute`. [More](https://github.com/andrei-m-code/halifax/blob/main/docs/authentication.md)

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

```csharp
public class PaymentClient(HttpClient http) : HalifaxHttpClient(http)
{
    public Task<PaymentDto> GetAsync(string id) =>
        SendAsync<PaymentDto>(CreateMessage(HttpMethod.Get, $"/api/payments/{id}"));
}

builder.Services.AddHalifaxHttpClient<PaymentClient>(
    defaultBaseUrl: "https://payments.api.com",
    defaultBearerToken: token);
```

`SendAsync<T>` deserializes the response and throws on a failed one. Resilience policies and correlation ID forwarding are one call each. [More](https://github.com/andrei-m-code/halifax/blob/main/docs/http-client.md)

## Logging

```csharp
L.Info("User {UserId} created", userId);
L.Error(exception, "Failed to process order {OrderId}", orderId);
```

`L` is a static Serilog console logger. ASP.NET Core's own logs are kept quiet; `ConfigureFrameworkLogging(LogLevel.Warning)` shows them. [More](https://github.com/andrei-m-code/halifax/blob/main/docs/logging.md)

## Excel & CSV

```bash
dotnet add package Halifax.Excel
```

```csharp
var converter = new ExcelConverter<Person>();
converter.AddMapping("Full Name", p => p.Name);

var people = await converter.ReadAsync(file.OpenReadStream(), file.ContentType);   // .xlsx, .xls or CSV

var stream = new MemoryStream();
await converter.WriteExcelAsync(stream, people, "People");   // stream stays open and is rewound
return File(stream, ExcelContentType.Xlsx, "People.xlsx");
```

`ExcelWorkbookWriter` puts several sheets of different types into one workbook (or rows whose columns are known only at run time), and `CsvFile` writes CSV from dictionary or `dynamic` rows. A bad cell throws `ExcelConversionException` with its row and column. Halifax.Excel does not need SkiaSharp. [More](https://github.com/andrei-m-code/halifax/blob/main/docs/excel.md)

## Utilities

```csharp
var encrypted = Crypto.Encrypt(secret, "sensitive data");   // CryptoFormat.Legacy unless you opt in to AesGcm
var decrypted = Crypto.Decrypt(secret, encrypted);         // reads both formats

var id = ShortId.Create();                                 // e.g. "kX9mBnQ"
var json = Json.Serialize(obj);
```

`Crypto.Encrypt(secret, text)` keeps writing the format every Halifax version reads; `CryptoFormat.AesGcm`, chosen per call, adds authenticated, randomized encryption for values that are decrypted and never compared. [More](https://github.com/andrei-m-code/halifax/blob/main/docs/utilities.md)

## Package Overview

| Package | Dependencies |
|---|---|
| **Halifax.Domain** | none |
| **Halifax.Core** | Halifax.Domain, Serilog, Serilog.Sinks.Console, Destructurama.Attributed, System.IdentityModel.Tokens.Jwt, Microsoft.Extensions.Logging |
| **Halifax.Api** | Halifax.Core, Swashbuckle.AspNetCore, Scalar.AspNetCore, Microsoft.AspNetCore.Authentication.JwtBearer |
| **Halifax.Http** | Halifax.Core, Microsoft.Extensions.Http.Resilience |
| **Halifax.Excel** | NPOI |

All packages target **.NET 11** with nullable reference types enabled. For older .NET versions see [Compatibility](#compatibility).

## MIT License

Copyright (c) 2020-2026 Andrei M

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

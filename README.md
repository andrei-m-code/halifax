| Package Name                      | NuGet                                                                                                         |
|-----------------------------------|---------------------------------------------------------------------------------------------------------------|
| Halifax.Api &nbsp;&nbsp;&nbsp;    | [![NuGet](https://img.shields.io/nuget/v/Halifax.Api.svg)](https://www.nuget.org/packages/Halifax.Api/)       |
| Halifax.Core &nbsp;&nbsp;&nbsp;   | [![NuGet](https://img.shields.io/nuget/v/Halifax.Core.svg)](https://www.nuget.org/packages/Halifax.Core/)     |
| Halifax.Http &nbsp;&nbsp;&nbsp;   | [![NuGet](https://img.shields.io/nuget/v/Halifax.Http.svg)](https://www.nuget.org/packages/Halifax.Http/)     |
| Halifax.Domain &nbsp;&nbsp;&nbsp; | [![NuGet](https://img.shields.io/nuget/v/Halifax.Domain.svg)](https://www.nuget.org/packages/Halifax.Domain/) |
| Halifax.Excel &nbsp;&nbsp;&nbsp;  | [![NuGet](https://img.shields.io/nuget/v/Halifax.Excel.svg)](https://www.nuget.org/packages/Halifax.Excel/)   |

# Halifax Service Foundation
Simplistic libraries for complex projects. Halifax libraries are designed to speed up API service development process by encapsulating common functionality required for all microservices, allowing developers to focus on the business logic instead of copy-pasting the boilerplate code from project to project. The libraries are focussed on the following aspects of any application:
- ✅ Exception handling
- ✅ JWT Authentication
- ✅ API models
- ✅ HTTP Communication
- ✅ Swagger
- ✅ Logging
- ✅ CORS

# Installation
Install the API library using [nuget package](https://www.nuget.org/packages/Halifax.Api) with Package Manager Console:

```
Install-Package Halifax.Api
```

Or using .NET CLI:

```
dotnet add package Halifax.Api
```

# Getting Started

Please explore [Peggy's Cove](https://github.com/andrei-m-code/halifax/blob/main/PeggysCove.Api/Program.cs) to get started as quickly as possible. For more details on certain topics, read the documentation. Here is what's needed to add Halifax to your project. In your Program.cs:

```csharp
using Halifax.Api;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHalifax();
var app = builder.Build();

app.UseHalifax();
app.Run("https://*:5000");
```

This enables routing with controllers and exception handling.

# Models

It's very beneficial when all the API responses follow the same format. It makes it easier to consume by the clients. In order to achieve it, there is a model called `ApiResponse`. It's designed to return response data, empty data or error information in the same consistent format. Here are the main use cases:

    // Return API response with your model
    return ApiResponse.With(model);
    
    // Return empty API response
    return ApiResponse.Empty;

When API response is used for all APIs in the project the response will always be of a format:

    {
        data: {...} // your model
        success: true/false,
        error: { // null if successful
            type: "ArgumentNullException",
            message: "Email is required",
            trace: "(126) ArgumentNullException was thrown ... (typical exception stack trace)"
        }
    }

# Exceptions

There are 3 main exception types that can be used out of the box:
- HalifaxException - resulting in 403 bad request. 
- HalifaxNotFoundException - 404 when resource is not found.
- HalifaxUnauthorizedException - 401 unauthorized.

These exceptions are handled by Halifax exception handling middleware. Typical use case can look like this:

```csharp
var user = await context.Users.FirstOrDefaultAsync(u => u.UserId == id);
if (user == null)
{
    throw new HalifaxNotFoundException("User not found");
}
```
The resulting HTTP response will have a status code 404 and JSON (using ApiResponse models):

```json
{
  "data": null,
  "success": false,
  "error": {
    "message": "User not found"
  }
}
```

For more advanced scenarios you can override DefaultExceptionHandler or implement and register your own IExceptionHandler.

# App Configuration

Halifax libraries are designed to work in containers. The most common way of configuring containers is by using Environment Variables. Halifax offers a few ways of working with it. Let's say we have env variables:

```dotenv
# It can also be a .env file in the root of your project. 
# Halifax always looks for .env to load it into the process. 
AppSettings__ConnectionString=localhost
AppSettings__HttpTimeout=120
```
Create class or a record (Yes, we support immutable records!):
```csharp
record AppSettings(string ConnectionString, int HttpTimeout);
```
When halifax is added, this is how settings are being registered:

```csharp
services.AddHalifax(builder => builder
    .AddSettings<AppSettings>()
    .AddSettings<AppSettings2>());
```
These calls do 3 things. They read the environment variables, map them to the setting classes or records and register these instances as singletons so that they can be injected into app services and controllers. 

If there is a place where you can't inject your settings, you can get them from the Env directly. This is a very cheap operation, that can be executed any number of times:

```csharp
var settings = Env.GetSection<AppSettings>();
```
If you need to load env variables from the file outside of the Halifax.Api, this is how it can be done:
```csharp
// Here is how to load variables from the console if
// Halifax.Core is used outside of the Halifax.Api
Env.Load(); // or
Env.Load("filename", swallowErrors: false);
```
Note: supported types are limited to primitives, DateTime, TimeSpan, Guid at the moment. Arrays aren't allowed either. If more types are required, please request it. 

# JWT Authentication

Enable authentication/authorization using the following code. When you AddHalifax dependencies to services on app startup, make this call:

```csharp
services.AddHalifax(builder => builder
    .ConfigureAuthentication("Your_JWT_secret",
        validateAudience: false,
        validateIssuer: false,
        requireExpirationTime: false));
```
When this is enabled all your non-`[AllowAnonymous]` will require "Authentication: Bearer {token}" header. Halifax provides a way to create tokens easily:

```csharp
var claims = new List<Claim> 
{
    new Claim("ClaimType", "ClaimData"),
    ...
};
var expiration = DateTime.UtcNow.AddDays(90);
var token = Jwt.Create("Your_JWT_secret", claims, expiration);
```
You can access request token data from `HttpContext.User.Claims`. To verify that claims are correct there is a helper `ClaimsAuthorizeFilterAttribute` to override, it can be used for methods and controllers:

```csharp
internal class MyClaimsAuthorize : ClaimsAuthorizeFilterAttribute
{
    protected override bool IsAuthorized(ActionExecutingContext context, List<Claim> claims)
    {
        // check your claims, return true/false or throw exception.
        // extension methods that can help:
        claims
            .ClaimExpected("ClaimType", "ClaimValue")
            .ClaimIsEmail("ClaimType", out var email);
            // etc.
        
        return true; // success
    }
}
```
If you need to read a token from string, use `Jwt.Read("Your_JWT_secret", token)`.

# MIT License

Copyright (c) 2020 Andrei M

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.


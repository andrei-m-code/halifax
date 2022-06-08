| Package Name | NuGet |
|-|-|
| Halifax.Api &nbsp;&nbsp;&nbsp; | [![NuGet](https://img.shields.io/nuget/v/Halifax.Api.svg)](https://www.nuget.org/packages/Halifax.Api/)  |
| Halifax.Core &nbsp;&nbsp;&nbsp; | [![NuGet](https://img.shields.io/nuget/v/Halifax.Core.svg)](https://www.nuget.org/packages/Halifax.Core/) |
| Halifax.Domain &nbsp;&nbsp;&nbsp; | [![NuGet](https://img.shields.io/nuget/v/Halifax.Domain.svg)](https://www.nuget.org/packages/Halifax.Domain/) |
| Halifax.Http &nbsp;&nbsp;&nbsp; | [![NuGet](https://img.shields.io/nuget/v/Halifax.Http.svg)](https://www.nuget.org/packages/Halifax.Http/) |

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

Please refer to the [Peggy's Cove](https://github.com/andrei-m-code/halifax/blob/main/PeggysCove.Api/Startup.cs) example API project Startup.cs file for more details but basically all you need is to have this in your startup class:

    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHalifax();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseHalifax();
        }
    }

This enables routing with controllers and exception handling.

# Models

It's very beneficial if all API responses follow the same format. It makes it easier to consume by the clients. In order to achieve it, there is a model called `ApiResponse`. It's designed to return response data, empty data or error information in the same consistent format. Here are the main use cases:

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

# Configuration

TODO: ...

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

**TODO**: Discribe how to read token and how to use custom authentication

# MIT License

Copyright (c) 2020 Andrei M

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.


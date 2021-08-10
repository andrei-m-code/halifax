| Package | NuGet |
|-|-|
| Halifax.Api  | [![NuGet](https://img.shields.io/nuget/v/Halifax.Api.svg)](https://www.nuget.org/packages/Halifax.Api/)  |
| Halifax.Core | [![NuGet](https://img.shields.io/nuget/v/Halifax.Core.svg)](https://www.nuget.org/packages/Halifax.Core/) |
| Halifax.Models | [![NuGet](https://img.shields.io/nuget/v/Halifax.Models.svg)](https://www.nuget.org/packages/Halifax.Models/) | 

# Halifax Service Foundation API
Halifax libraries are designed to speed up API service development process by encapsulating common functionality required for all microservices. In particular:
- ✅ Exception handling
- ✅ API models
- ✅ Swagger
- ✅ JWT Auth
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

# MIT License

Copyright (c) 2020 Andrei M

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.


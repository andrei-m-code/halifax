using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Halifax.Api.App;
using Halifax.Api.Middleware;
using Halifax.Core.Helpers;
using Halifax.Domain;
using Halifax.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace Halifax.Api.Tests;

[ApiController]
public class TestController : ControllerBase
{
    [HttpGet("test/ok")]
    public ApiResponse<string> Hello() => ApiResponse.With("hello");

    [HttpGet("test/not-found")]
    public ApiResponse NotFoundError() => throw new HalifaxNotFoundException("Nothing here");

    [HttpGet("test/bad-request")]
    public ApiResponse BadRequestError() => throw new HalifaxException("Bad input");

    [Authorize]
    [HttpGet("test/secure")]
    public ApiResponse<string> Secure() => ApiResponse.With("secret");
}

/// <summary>
/// Boots a real ASP.NET Core app with AddHalifax/UseHalifax on an in-memory test server and checks the
/// pieces that only fail at runtime: OpenAPI generation (Swashbuckle), the Scalar UI, JWT auth,
/// exception mapping and correlation IDs. Halifax allows one app per process, so all tests share it.
/// </summary>
[TestFixture]
public class HalifaxApiTests
{
    private const string jwtSecret = "halifax-api-tests-secret-that-is-long-enough-for-hs256";

    private WebApplication app = null!;
    private HttpClient client = null!;

    [OneTimeSetUp]
    public async Task StartApp()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddHalifax(halifax => halifax
            .SetName("Halifax Api Tests")
            .ConfigureAuthentication(jwtSecret)
            .ConfigureMvc(mvc => mvc.AddApplicationPart(typeof(TestController).Assembly)));

        app = builder.Build();
        app.UseHalifax();
        await app.StartAsync();
        client = app.GetTestClient();
    }

    [OneTimeTearDown]
    public async Task StopApp()
    {
        client.Dispose();
        await app.StopAsync();
        await app.DisposeAsync();
    }

    [Test]
    public async Task OpenApiDocument_IsGenerated()
    {
        var response = await client.GetAsync("/openapi/v1.json");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;

        Assert.That(root.GetProperty("openapi").GetString(), Does.StartWith("3."));
        Assert.That(root.GetProperty("info").GetProperty("title").GetString(), Is.EqualTo("Halifax Api Tests"));

        var paths = root.GetProperty("paths");
        Assert.That(paths.TryGetProperty("/halifax/version", out _), Is.True);
        Assert.That(paths.TryGetProperty("/test/ok", out _), Is.True);

        var schemes = root.GetProperty("components").GetProperty("securitySchemes");
        Assert.That(schemes.TryGetProperty("Bearer JWT", out _), Is.True);
    }

    [Test]
    public async Task ScalarUi_IsServed()
    {
        var response = await client.GetAsync("/scalar");
        for (var hop = 0; hop < 3 && (int)response.StatusCode is >= 300 and < 400; hop++)
        {
            response = await client.GetAsync(response.Headers.Location);
        }

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("text/html"));
    }

    [Test]
    public async Task VersionEndpoint_ListsHalifaxAssemblies()
    {
        var response = await client.GetAsync("/halifax/version");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var body = await response.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("Halifax.Api"));
        Assert.That(body, Does.Contain("Halifax.Core"));
    }

    [Test]
    public async Task Controller_ReturnsApiResponse()
    {
        var response = await client.GetAsync("/test/ok");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.That(GetProperty(document.RootElement, "success").GetBoolean(), Is.True);
        Assert.That(GetProperty(document.RootElement, "data").GetString(), Is.EqualTo("hello"));
    }

    [TestCase("/test/not-found", HttpStatusCode.NotFound, "HalifaxNotFoundException")]
    [TestCase("/test/bad-request", HttpStatusCode.BadRequest, "HalifaxException")]
    public async Task HalifaxExceptions_MapToStatusCodes(string path, HttpStatusCode expected, string errorType)
    {
        var response = await client.GetAsync(path);

        Assert.That(response.StatusCode, Is.EqualTo(expected));
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.That(GetProperty(document.RootElement, "success").GetBoolean(), Is.False);
        Assert.That(GetProperty(GetProperty(document.RootElement, "error"), "type").GetString(), Is.EqualTo(errorType));
    }

    [Test]
    public async Task SecureEndpoint_WithoutToken_IsUnauthorized()
    {
        var response = await client.GetAsync("/test/secure");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task SecureEndpoint_WithValidToken_IsAllowed()
    {
        var token = Jwt.Create(jwtSecret, [new Claim("sub", "test-user")], DateTime.UtcNow.AddMinutes(5));
        using var request = new HttpRequestMessage(HttpMethod.Get, "/test/secure");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.SendAsync(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task CorrelationId_IsEchoedBack()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/test/ok");
        request.Headers.Add(CorrelationIdMiddleware.HeaderName, "test-correlation-123");

        var response = await client.SendAsync(request);

        Assert.That(response.Headers.GetValues(CorrelationIdMiddleware.HeaderName).Single(),
            Is.EqualTo("test-correlation-123"));
    }

    [Test]
    public async Task ErrorResponse_CarriesTheTraceByDefault()
    {
        var response = await client.GetAsync("/test/bad-request");

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var error = GetProperty(document.RootElement, "error");
        Assert.That(GetProperty(error, "message").GetString(), Is.EqualTo("Bad input"));
        Assert.That(GetProperty(error, "trace").GetString(), Does.Contain(nameof(TestController.BadRequestError)));
    }

    [Test]
    public async Task ErrorTrace_CanBeTurnedOff_KeepingTheShape()
    {
        HalifaxBuilder.Instance!.ConfigureErrorTrace(false);
        try
        {
            var response = await client.GetAsync("/test/bad-request");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var error = GetProperty(document.RootElement, "error");
            Assert.That(GetProperty(error, "type").GetString(), Is.EqualTo("HalifaxException"));
            Assert.That(GetProperty(error, "message").GetString(), Is.EqualTo("Bad input"));
            Assert.That(GetProperty(error, "trace").ValueKind, Is.EqualTo(JsonValueKind.Null));
        }
        finally
        {
            HalifaxBuilder.Instance!.ConfigureErrorTrace(true);
        }
    }

    [Test]
    public void FrameworkLogging_DefaultsToCritical()
    {
        var rules = app.Services.GetRequiredService<IOptions<LoggerFilterOptions>>().Value.Rules;

        Assert.That(rules.Last(r => r.CategoryName == "Microsoft").LogLevel, Is.EqualTo(LogLevel.Critical));
        Assert.That(rules.Last(r => r.CategoryName == "System").LogLevel, Is.EqualTo(LogLevel.Critical));
    }

    [Test]
    public void FrameworkLogging_LevelCanBeLowered()
    {
        var services = new ServiceCollection();
        services.CleanupDefaultLogging(LogLevel.Warning);

        using var provider = services.BuildServiceProvider();
        var rules = provider.GetRequiredService<IOptions<LoggerFilterOptions>>().Value.Rules;

        Assert.That(rules.Last(r => r.CategoryName == "Microsoft").LogLevel, Is.EqualTo(LogLevel.Warning));
        Assert.That(rules.Last(r => r.CategoryName == "System").LogLevel, Is.EqualTo(LogLevel.Warning));
    }

    [Test]
    public void ApiResponseError_TraceIsOptional()
    {
        var exception = CaughtException();

        Assert.That(ApiResponseError.IncludeTraceByDefault, Is.True);
        Assert.That(ApiResponse.With(exception).Error!.Trace, Is.Not.Null);
        Assert.That(ApiResponse.With(exception, includeTrace: true).Error!.Trace, Is.Not.Null);
        Assert.That(ApiResponse.With(exception, includeTrace: false).Error!.Trace, Is.Null);
        Assert.That(ApiResponse.With(exception, includeTrace: false).Error!.Message, Is.EqualTo("boom"));
    }

    [Test]
    public void ErrorTraceSetting_ReachesCustomHandlersToo()
    {
        // Custom handlers (e.g. a 409 handler) build their body with ApiResponse.With(exception).
        HalifaxBuilder.Instance!.ConfigureErrorTrace(false);
        try
        {
            Assert.That(ApiResponse.With(CaughtException()).Error!.Trace, Is.Null);
            Assert.That(new ApiResponseError(CaughtException()).Trace, Is.Null);
        }
        finally
        {
            HalifaxBuilder.Instance!.ConfigureErrorTrace(true);
        }

        Assert.That(ApiResponse.With(CaughtException()).Error!.Trace, Is.Not.Null);
    }

    private static Exception CaughtException()
    {
        try
        {
            throw new HalifaxException("boom");
        }
        catch (Exception e)
        {
            return e;
        }
    }

    private static JsonElement GetProperty(JsonElement element, string name)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return property.Value;
            }
        }

        throw new KeyNotFoundException($"Property '{name}' not found in {element}");
    }
}
